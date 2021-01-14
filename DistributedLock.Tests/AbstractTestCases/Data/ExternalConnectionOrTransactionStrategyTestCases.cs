using NUnit.Framework;
using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Data
{
    public abstract class ExternalConnectionOrTransactionStrategyTestCases<TLockProvider, TStrategy, TDb>
        where TLockProvider : TestingLockProvider<TStrategy>, new()
        where TStrategy : TestingExternalConnectionOrTransactionSynchronizationStrategy<TDb>, new()
        where TDb : ITestingDb, new()
    {
        private TLockProvider _lockProvider = default!;

        [SetUp] public void SetUp() => this._lockProvider = new TLockProvider();
        [TearDown] public void TearDown() => this._lockProvider.Dispose();

        [Test]
        [NonParallelizable, Retry(tryCount: 3)] // timing sensitive for SqlSemaphore (see comment in that file regarding the 32ms wait)
        public void TestDeadlockDetection()
        {
            var timeout = TimeSpan.FromSeconds(15);

            using var barrier = new Barrier(participantCount: 2);
            const string LockName1 = nameof(TestDeadlockDetection) + "_1",
                LockName2 = nameof(TestDeadlockDetection) + "_2";

            Task RunDeadlock(bool isFirst)
            {
                this._lockProvider.Strategy.StartAmbient();
                var lock1 = this._lockProvider.CreateLock(isFirst ? LockName1 : LockName2);
                var lock2 = this._lockProvider.CreateLock(isFirst ? LockName2 : LockName1);
                return Task.Run(async () =>
                {
                    using (await lock1.AcquireAsync(timeout))
                    {
                        barrier.SignalAndWait();
                        (await lock2.AcquireAsync(timeout)).Dispose();
                    }
                });
            }

            var tasks = new[] { RunDeadlock(isFirst: true), RunDeadlock(isFirst: false) };

            Task.WhenAll(tasks).ContinueWith(_ => { }).Wait(TimeSpan.FromSeconds(10)).ShouldEqual(true, this.GetType().Name);

            var deadlockVictim = tasks.Single(t => t.IsFaulted);
            Assert.IsInstanceOf<InvalidOperationException>(deadlockVictim.Exception!.GetBaseException()); // backwards compat check
            Assert.IsInstanceOf<DeadlockException>(deadlockVictim.Exception.GetBaseException());

            tasks.Count(t => t.Status == TaskStatus.RanToCompletion).ShouldEqual(1);
        }

        [Test]
        public async Task TestReAcquireLockOnSameConnection()
        {
            var @lock = this._lockProvider.CreateLock("lock");
            await using var handle = await @lock.AcquireAsync();
            Assert.ThrowsAsync<DeadlockException>(() => @lock.AcquireAsync().AsTask());
            Assert.ThrowsAsync<TimeoutException>(() => @lock.AcquireAsync(TimeSpan.FromSeconds(.01)).AsTask());
        }

        /// <summary>
        /// Currently, we leverage <see cref="DbConnection.StateChange"/> to track handle loss. This test
        /// validates that the handler is properly removed when the lock handle is disposed
        /// </summary>
        [Test]
        public void TestStateChangeHandlerIsNotLeaked()
        {
            this._lockProvider.Strategy.StartAmbient();

            // creating this first assures that the Semaphore5 provider's handlers get included in initial
            var @lock = this._lockProvider.CreateLock(nameof(TestStateChangeHandlerIsNotLeaked));

            var initialHandler = GetStateChanged(this._lockProvider.Strategy.AmbientConnection!);

            using (@lock.Acquire())
            {
                Assert.IsNotNull(GetStateChanged(this._lockProvider.Strategy.AmbientConnection!));
            }

            GetStateChanged(this._lockProvider.Strategy.AmbientConnection!).ShouldEqual(initialHandler);

            static StateChangeEventHandler? GetStateChanged(DbConnection connection) =>
                (StateChangeEventHandler?)typeof(DbConnection).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                    .Single(f => f.FieldType == typeof(StateChangeEventHandler))
                    .GetValue(connection);
        }
    }
}
