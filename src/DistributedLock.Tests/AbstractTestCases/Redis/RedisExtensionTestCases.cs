using NUnit.Framework;

namespace Medallion.Threading.Tests.Redis;

public abstract class RedisExtensionTestCases<TLockProvider, TDatabaseProvider>
    where TLockProvider : TestingLockProvider<TestingRedisSynchronizationStrategy<TDatabaseProvider>>, new()
    where TDatabaseProvider : TestingRedisDatabaseProvider, new()
{
    private TLockProvider _provider = default!;

    [SetUp]
    public async Task SetUp()
    {
        this._provider = new TLockProvider();
        await this._provider.SetupAsync();
    }
    [TearDown]
    public async Task TearDown() => await this._provider.DisposeAsync();

    [Test]
    [NonParallelizable, Retry(tryCount: 3)] // timing-sensitive
    public async Task TestCanExtendLock()
    {
        this._provider.Strategy.SetOptions(o => o.Expiry(TimeSpan.FromSeconds(1)).BusyWaitSleepTime(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50)));
        var @lock = this._provider.CreateLock(Guid.NewGuid().ToString());

        await using var handle = await @lock.AcquireAsync();

        var secondHandleTask = @lock.AcquireAsync().AsTask();
        _ = secondHandleTask.ContinueWith(t => t.Result.Dispose()); // ensure cleanup
        Assert.That(await secondHandleTask.TryWaitAsync(TimeSpan.FromSeconds(2)), Is.False);

        await handle.DisposeAsync();

        Assert.That(await secondHandleTask.TryWaitAsync(TimeSpan.FromSeconds(5)), Is.True);
    }
}
