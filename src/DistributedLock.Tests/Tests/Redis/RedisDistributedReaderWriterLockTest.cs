using Medallion.Threading.Redis;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Medallion.Threading.Tests.Redis;

public class RedisDistributedReaderWriterLockTest
{
    [Test]
    [Category("CI")]
    public void TestName()
    {
        const string Name = "\0🐉汉字\b\r\n\\";
        var @lock = new RedisDistributedReaderWriterLock(Name, new Mock<IDatabase>(MockBehavior.Strict).Object);
        @lock.Name.ShouldEqual(Name);
    }

    [Test]
    [Category("CI")]
    public void TestValidatesConstructorParameters()
    {
        var database = new Mock<IDatabase>(MockBehavior.Strict).Object;
        Assert.Throws<ArgumentNullException>(() => new RedisDistributedReaderWriterLock(default!, database));
        Assert.Throws<ArgumentNullException>(() => new RedisDistributedReaderWriterLock(default!, new[] { database }));
        Assert.Throws<ArgumentNullException>(() => new RedisDistributedReaderWriterLock("key", default(IDatabase)!));
        Assert.Throws<ArgumentNullException>(() => new RedisDistributedReaderWriterLock("key", default(IEnumerable<IDatabase>)!));
        Assert.Throws<ArgumentNullException>(() => new RedisDistributedReaderWriterLock("key", new[] { database, null! }));
        Assert.Throws<ArgumentException>(() => new RedisDistributedReaderWriterLock("key", Enumerable.Empty<IDatabase>()));
        Assert.Throws<ArgumentException>(() => new RedisDistributedReaderWriterLock("key", new[] { database }, o => o.Expiry(TimeSpan.FromSeconds(0.2))));
    }

    [Test]
    [NonParallelizable] // timing-sensitive
    public async Task TestCanExtendReadLock()
    {
        var @lock = new RedisDistributedReaderWriterLock(
            TestHelper.UniqueName,
            RedisServer.CreateDatabase(RedisSetUpFixture.Redis),
            o => o.Expiry(TimeSpan.FromSeconds(0.3)).BusyWaitSleepTime(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(5))
        );

        await using var readHandle = await @lock.AcquireReadLockAsync();

        var writeHandleTask = @lock.AcquireWriteLockAsync().AsTask();
        _ = writeHandleTask.ContinueWith(t => t.Result.Dispose()); // ensure cleanup
        Assert.That(await writeHandleTask.TryWaitAsync(TimeSpan.FromSeconds(.5)), Is.False);

        await readHandle.DisposeAsync();

        Assert.That(await writeHandleTask.TryWaitAsync(TimeSpan.FromSeconds(5)), Is.True);
    }

    [Test]
    [NonParallelizable] // timing-sensitive
    public async Task TestReadLockAbandonment()
    {
        var @lock = new RedisDistributedReaderWriterLock(
            TestHelper.UniqueName,
            RedisServer.CreateDatabase(RedisSetUpFixture.Redis),
            o => o.Expiry(TimeSpan.FromSeconds(1))
                .ExtensionCadence(TimeSpan.FromSeconds(0.1))
                .BusyWaitSleepTime(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(50))
        );

        await AcquireReadLockAsync();
        await Task.Delay(20); // seems to help ensure that the GC works
        GC.Collect();
        GC.WaitForPendingFinalizers();

        await using var writeHandle = await @lock.TryAcquireWriteLockAsync(TimeSpan.FromSeconds(10));
        Assert.That(writeHandle, Is.Not.Null); // indicates read lock was released

        async Task AcquireReadLockAsync() => await @lock.AcquireReadLockAsync();
    }
}
