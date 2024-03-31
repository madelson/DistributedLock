using NUnit.Framework;
using System.Diagnostics;

namespace Medallion.Threading.Tests.Data;

public abstract class ConnectionStringStrategyTestCases<TLockProvider, TStrategy, TDb>
    where TLockProvider : TestingLockProvider<TStrategy>, new()
    where TStrategy : TestingConnectionStringSynchronizationStrategy<TDb>, new()
    where TDb : TestingPrimaryClientDb, new()
{
    private TLockProvider _lockProvider = default!;

    [SetUp] public void SetUp() => this._lockProvider = new TLockProvider();
    [TearDown] public void TearDown() => this._lockProvider.Dispose();

    /// <summary>
    /// Tests that internally-owned connections are properly cleaned up by disposing the lock handle 
    /// </summary>
    [Test]
    public void TestConnectionDoesNotLeak()
    {
        // If the lock is based on a multi-ticket semaphore, then the first creation will claim N-1 connections. To avoid this messing with
        // our count, we create a throwaway lock instance here to hold those connections using the default application name
        this._lockProvider.CreateLock(nameof(TestConnectionDoesNotLeak));

        // set a distinctive application name so that we can count how many connections are used
        var applicationName = this._lockProvider.Strategy.Db.SetUniqueApplicationName();

        var @lock = this._lockProvider.CreateLock(nameof(TestConnectionDoesNotLeak));
        for (var i = 0; i < 30; ++i)
        {
            using (@lock.Acquire())
            {
                this._lockProvider.Strategy.Db.CountActiveSessions(applicationName).ShouldEqual(1, this.GetType().Name);
            }
            // still alive due to pooling, except in Oracle where the application name (client info) is not part of the pool key
            Assert.That(this._lockProvider.Strategy.Db.CountActiveSessions(applicationName), Is.LessThanOrEqualTo(1), this.GetType().Name);
        }

        using (var connection = this._lockProvider.Strategy.Db.CreateConnection())
        {
            this._lockProvider.Strategy.Db.ClearPool(connection);
        }

        // checking immediately seems flaky; likely clear pool finishing
        // doesn't guarantee that SQL will immediately reflect the clear
        var maxWaitForPoolsToClear = TimeSpan.FromSeconds(5);
        var stopwatch = Stopwatch.StartNew();
        do
        {
            var activeCount = this._lockProvider.Strategy.Db.CountActiveSessions(applicationName);
            if (activeCount == 0) { return; }
            Thread.Sleep(10);
        }
        while (stopwatch.Elapsed < maxWaitForPoolsToClear);

        Assert.Fail("Connection was not released");
    }

    [Test]
    [NonParallelizable, Retry(5)] // timing-sensitive
    public void TestKeepaliveProtectsFromIdleSessionKiller()
    {
        var applicationName = this._lockProvider.Strategy.Db.SetUniqueApplicationName();

        this._lockProvider.Strategy.KeepaliveCadence = TimeSpan.FromSeconds(.05);
        var @lock = this._lockProvider.CreateLock(Guid.NewGuid().ToString()); // use unique name due to retry
            
        var handle = @lock.Acquire();
        using var idleSessionKiller = new IdleSessionKiller(this._lockProvider.Strategy.Db, applicationName, idleTimeout: TimeSpan.FromSeconds(.5));
        Thread.Sleep(TimeSpan.FromSeconds(2));
        Assert.DoesNotThrow(handle.Dispose);
    }

    /// <summary>
    /// Demonstrates that we don't multi-thread the connection despite running keepalive queries
    /// </summary>
    [Test]
    public void TestKeepaliveDoesNotCreateRaceCondition()
    {
        this._lockProvider.Strategy.KeepaliveCadence = TimeSpan.FromMilliseconds(1);

        Assert.DoesNotThrow(() =>
        {
            var @lock = this._lockProvider.CreateLock(nameof(TestKeepaliveDoesNotCreateRaceCondition));
            for (var i = 0; i < 25; ++i)
            {
                using (@lock.Acquire())
                {
                    Thread.Sleep(1);
                }
            }
        });
    }

    // replicates issue from https://github.com/madelson/DistributedLock/issues/85
    [Test]
    public async Task TestAccessingHandleLostTokenWhileKeepaliveActiveDoesNotBlock()
    {
        this._lockProvider.Strategy.KeepaliveCadence = TimeSpan.FromMinutes(5);

        var @lock = this._lockProvider.CreateLock(string.Empty);
        var handle = await @lock.TryAcquireAsync();
        if (handle != null)
        {
            var accessHandleLostTokenTask = Task.Run(() =>
            {
                if (handle.HandleLostToken.CanBeCanceled)
                {
                    handle.HandleLostToken.Register(() => { });
                }
            });
            Assert.That(await accessHandleLostTokenTask.TryWaitAsync(TimeSpan.FromSeconds(5)), Is.True);

            // do this only on success; on failure we're likely deadlocked and dispose will hang
            await handle.DisposeAsync();
        }
    }
}
