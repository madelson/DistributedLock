using Medallion.Threading.Internal;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Medallion.Threading.Tests.Data;

public abstract class OwnedConnectionStrategyTestCases<TLockProvider, TDb>
    where TLockProvider : TestingLockProvider<TestingOwnedConnectionSynchronizationStrategy<TDb>>, new()
    where TDb : TestingPrimaryClientDb, new()
{
    private TLockProvider _lockProvider = default!;

    [SetUp] public void SetUp() => this._lockProvider = new TLockProvider();
    [TearDown] public void TearDown() => this._lockProvider.Dispose();

    /// <summary>
    /// Tests that our idle session killer works, therefore validating our other tests that use it.
    /// 
    /// We test this here rather than in <see cref="ConnectionStringStrategyTestCases{TLockProvider, TStrategy, TDb}"/>
    /// because (a) we don't need to repeat the test for both regular and multiplexed and (2) for owned-transaction the test won't
    /// pass because you can safely Dispose a transaction on a killed SQL connection
    /// </summary>
    [Test]
    public void TestIdleSessionKiller()
    {
        // This makes sure that for the Semaphore5 lock initial 4 tickets are taken with the default
        // application name and therefore won't be counted or killed
        this._lockProvider.CreateLock(nameof(TestIdleSessionKiller));

        var applicationName = this._lockProvider.Strategy.Db.SetUniqueApplicationName();
        var @lock = this._lockProvider.CreateLock(nameof(TestIdleSessionKiller));

        // go through one acquire/dispose cycle to ensure all commands are prepared. Due to 
        // https://github.com/npgsql/npgsql/issues/2912 in Postgres, we get NRE on the post-kill Dispose()
        // call rather than the DbException we expected.
        @lock.Acquire().Dispose();

        using var handle = @lock.Acquire();
        this._lockProvider.Strategy.Db.CountActiveSessions(applicationName).ShouldEqual(1);

        using var idleSessionKiller = new IdleSessionKiller(this._lockProvider.Strategy.Db, applicationName, idleTimeout: TimeSpan.FromSeconds(.1));
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            Thread.Sleep(TimeSpan.FromSeconds(.02));
            if (this._lockProvider.Strategy.Db.CountActiveSessions(applicationName) == 0)
            {
                break;
            }
            if (stopwatch.Elapsed > TimeSpan.FromSeconds(5))
            {
                Assert.Fail("Timed out waiting for idle session to be killed");
            }
        }

        Assert.Catch<DbException>(() => handle.Dispose());
    }
}
