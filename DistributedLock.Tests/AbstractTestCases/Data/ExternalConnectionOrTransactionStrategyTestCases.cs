using NUnit.Framework;
using System;
using System.Linq;
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
        public void TestDeadlockDetection()
        {
            var timeout = TimeSpan.FromSeconds(30);

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
    }
}
