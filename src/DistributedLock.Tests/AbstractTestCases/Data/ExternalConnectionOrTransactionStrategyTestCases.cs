using NUnit.Framework;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace Medallion.Threading.Tests.Data;

public abstract class ExternalConnectionOrTransactionStrategyTestCases<TLockProvider, TStrategy, TDb>
    where TLockProvider : TestingLockProvider<TStrategy>, new()
    where TStrategy : TestingExternalConnectionOrTransactionSynchronizationStrategy<TDb>, new()
    where TDb : TestingDb, new()
{
    private TLockProvider _lockProvider = default!;

    [SetUp] public void SetUp() => this._lockProvider = new TLockProvider();
    [TearDown] public void TearDown() => this._lockProvider.Dispose();

    [Test]
    [NonParallelizable, Retry(tryCount: 3)] // timing sensitive for SqlSemaphore (see comment in that file regarding the 32ms wait)
    public void TestDeadlockDetection()
    {
        var timeout = TimeSpan.FromSeconds(20);

        using var barrier = new Barrier(participantCount: 2);
        const string LockName1 = nameof(TestDeadlockDetection) + "_1",
            LockName2 = nameof(TestDeadlockDetection) + "_2";

        Task RunDeadlockAsync(bool isFirst)
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

        var tasks = new[] { RunDeadlockAsync(isFirst: true), RunDeadlockAsync(isFirst: false) };

        Task.WhenAll(tasks).ContinueWith(_ => { }).Wait(TimeSpan.FromSeconds(15)).ShouldEqual(true, this.GetType().Name);

        // MariaDB fails both tasks due to deadlock instead of just picking a single victim
        Assert.That(tasks.Count(t => t.IsFaulted), Is.GreaterThanOrEqualTo(1));
        Assert.That(tasks.Count(t => t.Status == TaskStatus.RanToCompletion), Is.LessThanOrEqualTo(1));
        Assert.That(tasks.Where(t => t.IsCanceled), Is.Empty);

        foreach (var deadlockVictim in tasks.Where(t => t.IsFaulted))
        {
            Assert.That(deadlockVictim.Exception!.GetBaseException(), Is.InstanceOf<InvalidOperationException>()); // backwards compat check
            Assert.That(deadlockVictim.Exception.GetBaseException(), Is.InstanceOf<DeadlockException>());
        }
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
            Assert.That(GetStateChanged(this._lockProvider.Strategy.AmbientConnection!), Is.Not.Null);
        }

        GetStateChanged(this._lockProvider.Strategy.AmbientConnection!).ShouldEqual(initialHandler);

        static StateChangeEventHandler? GetStateChanged(DbConnection connection) =>
            // We check both the connection type and the base type because OracleConnection overrides the storage for
            // the StateChange event handler
            (StateChangeEventHandler?)new[] { connection.GetType(), typeof(DbConnection) }
                .Select(
                    t => t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                        .Where(f => f.FieldType == typeof(StateChangeEventHandler))
                        .SingleOrDefault()
                )
                .First(f => f != null)!
                .GetValue(connection);
    }
}
