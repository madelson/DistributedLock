using NUnit.Framework;

namespace Medallion.Threading.Tests.Redis;

public abstract class RedisExtensionTestCases<TLockProvider, TDatabaseProvider>
    where TLockProvider : TestingLockProvider<TestingRedisSynchronizationStrategy<TDatabaseProvider>>, new()
    where TDatabaseProvider : TestingRedisDatabaseProvider, new()
{
    private TLockProvider _provider = default!;

    [SetUp]
    public void SetUp() => this._provider = new TLockProvider();

    [TearDown]
    public void TearDown() => this._provider.Dispose();

    [Test]
    [NonParallelizable, Retry(tryCount: 3)] // timing-sensitive
    public async Task TestCanExtendLock()
    {
        this._provider.Strategy.SetOptions(o => o.Expiry(TimeSpan.FromSeconds(1)).BusyWaitSleepTime(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50)));
        var @lock = this._provider.CreateLock(Guid.NewGuid().ToString());

        await using var handle = await @lock.AcquireAsync();

        var secondHandleTask = @lock.AcquireAsync().AsTask();
        _ = secondHandleTask.ContinueWith(t => t.Result.Dispose()); // ensure cleanup
        Assert.IsFalse(await secondHandleTask.WaitAsync(TimeSpan.FromSeconds(2)));

        await handle.DisposeAsync();

        Assert.IsTrue(await secondHandleTask.WaitAsync(TimeSpan.FromSeconds(5)));
    }
}
