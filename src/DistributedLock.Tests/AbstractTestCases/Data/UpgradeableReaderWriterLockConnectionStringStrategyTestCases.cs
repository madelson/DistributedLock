using NUnit.Framework;

namespace Medallion.Threading.Tests.Data;

public abstract class UpgradeableReaderWriterLockConnectionStringStrategyTestCases<TLockProvider, TStrategy, TDb>
    where TLockProvider : TestingUpgradeableReaderWriterLockProvider<TStrategy>, new()
    where TStrategy : TestingConnectionStringSynchronizationStrategy<TDb>, new()
    where TDb : TestingPrimaryClientDb, new()
{
    private TLockProvider _lockProvider = default!;

    [SetUp]
    public async Task SetUp()
    {
        this._lockProvider = new TLockProvider();
        await this._lockProvider.SetupAsync();
    }
    [TearDown]
    public async Task TearDown() => await this._lockProvider.DisposeAsync();

    /// <summary>
    /// Tests the logic where upgrading a connection stops and restarts the keepalive
    /// </summary>
    [Test]
    [NonParallelizable, Retry(tryCount: 5)] // this test is somewhat timing sensitive
    public void TestKeepaliveProtectsFromIdleSessionKillerAfterFailedUpgrade()
    {
        var applicationName = this._lockProvider.Strategy.Db.SetUniqueApplicationName();

        this._lockProvider.Strategy.KeepaliveCadence = TimeSpan.FromSeconds(.1);
        var @lock = this._lockProvider.CreateUpgradeableReaderWriterLock(Guid.NewGuid().ToString());

        using var idleSessionKiller = new IdleSessionKiller(this._lockProvider.Strategy.Db, applicationName, idleTimeout: TimeSpan.FromSeconds(2));

        using (@lock.AcquireReadLock())
        {
            var handle = @lock.AcquireUpgradeableReadLock();
            handle.TryUpgradeToWriteLock().ShouldEqual(false);
            handle.TryUpgradeToWriteLockAsync().Result.ShouldEqual(false);
            Thread.Sleep(TimeSpan.FromSeconds(4));
            Assert.DoesNotThrow(() => handle.Dispose());
        }
    }

    /// <summary>
    /// Demonstrates that we don't multi-thread the connection despite running keepalive queries
    /// 
    /// This test is similar to <see cref="ConnectionStringStrategyTestCases{TLockProvider, TStrategy, TDb}.TestKeepaliveDoesNotCreateRaceCondition"/>,
    /// but in this case we additionally test lock upgrading which must pause and restart the keepalive process.
    /// </summary>
    [Test]
    public void TestKeepaliveDoesNotCreateRaceCondition()
    {
        this._lockProvider.Strategy.KeepaliveCadence = TimeSpan.FromMilliseconds(1);

        Assert.DoesNotThrow(() =>
        {
            var @lock = this._lockProvider.CreateUpgradeableReaderWriterLock(nameof(TestKeepaliveDoesNotCreateRaceCondition));
            for (var i = 0; i < 30; ++i)
            {
                using var handle = @lock.AcquireUpgradeableReadLockAsync().Result;
                Thread.Sleep(1);
                handle.UpgradeToWriteLock();
                Thread.Sleep(1);
            }
        });
    }
}
