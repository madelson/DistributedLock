using Medallion.Threading.Redis;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;
using System.Globalization;

namespace Medallion.Threading.Tests.Redis;

public class RedisDistributedLockTest
{
    [Test, Category("CI")]
    public void TestName()
    {
        const string Name = "\0🐉汉字\b\r\n\\";
        var @lock = new RedisDistributedLock(Name, new Mock<IDatabase>(MockBehavior.Strict).Object);
        @lock.Name.ShouldEqual(Name);
        @lock.Key.ShouldEqual(new RedisKey(Name));
    }

    [Test, Category("CI")]
    public void TestValidatesConstructorParameters()
    {
        var database = new Mock<IDatabase>(MockBehavior.Strict).Object;
        Assert.Throws<ArgumentNullException>(() => new RedisDistributedLock(default, database));
        Assert.Throws<ArgumentNullException>(() => new RedisDistributedLock(default, new[] { database }));
        Assert.Throws<ArgumentNullException>(() => new RedisDistributedLock("key", default(IDatabase)!));
        Assert.Throws<ArgumentNullException>(() => new RedisDistributedLock("key", default(IEnumerable<IDatabase>)!));
        Assert.Throws<ArgumentNullException>(() => new RedisDistributedLock("key", new[] { database, null! }));
        Assert.Throws<ArgumentException>(() => new RedisDistributedLock("key", Enumerable.Empty<IDatabase>()));
    }

    /// <summary>
    /// Reproduces the bug in https://github.com/madelson/DistributedLock/issues/162
    /// where a Redis lock couldn't be acquired if the current CultureInfo was tr-TR,
    /// due to a bug in the underlying StackExchange.Redis package.
    /// 
    /// This is because there are both "dotted i" and "dotless i" in some Turkic languages:
    /// https://en.wikipedia.org/wiki/Dotted_and_dotless_I_in_computing
    /// </summary>
    [Test]
    public async Task TestCanAcquireLockWhenCurrentCultureIsTurkishTurkey()
    {
        var originalCultureInfo = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            var @lock = new RedisDistributedLock(
                TestHelper.UniqueName,
                RedisServer.GetDefaultServer(0).Multiplexer.GetDatabase()
            );
            await (await @lock.AcquireAsync()).DisposeAsync();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCultureInfo;
        }
    }

    [Test]
    public async Task TestGetCurrentCountReflectsAcquisitionsAndReleases()
    {
        var _db = RedisServer.GetDefaultServer(0).Multiplexer.GetDatabase();

        const int maxCount = 3;
        var key = TestHelper.UniqueName + ":sem";
        var semaphore = new RedisDistributedSemaphore(key, maxCount, _db);


        maxCount.ShouldEqual(semaphore.GetCurrentCount());
        maxCount.ShouldEqual(await semaphore.GetCurrentCountAsync());

        // Acquire one
        var handle1 = await semaphore.AcquireAsync();
     
        Assert.IsNotNull(handle1);
        Assert.That(semaphore.GetCurrentCount(), Is.EqualTo(maxCount - 1));
        Assert.That(await semaphore.GetCurrentCountAsync(), Is.EqualTo(maxCount - 1));

        // Acquire second
        var handle2 = await semaphore.AcquireAsync();
        Assert.IsNotNull(handle2);
        Assert.That(semaphore.GetCurrentCount(), Is.EqualTo(maxCount - 2));
        Assert.That(await semaphore.GetCurrentCountAsync(), Is.EqualTo(maxCount - 2));

        // Release first
        await handle1.DisposeAsync();
        Assert.That(semaphore.GetCurrentCount(), Is.EqualTo(maxCount - 1));
        Assert.That(await semaphore.GetCurrentCountAsync(), Is.EqualTo(maxCount - 1));

        // Release second
        await handle2.DisposeAsync();
        Assert.That(semaphore.GetCurrentCount(), Is.EqualTo(maxCount));
        Assert.That(await semaphore.GetCurrentCountAsync(), Is.EqualTo(maxCount));
    }

    [Test]
    public async Task TestGetCurrentCountNeverNegativeWhenOverReleased()
    {
        var _db = RedisServer.GetDefaultServer(0).Multiplexer.GetDatabase();

        const int maxCount = 2;
        var key = TestHelper.UniqueName + ":sem2";
        var semaphore = new RedisDistributedSemaphore(key, maxCount, _db);

        // Acquire more than maxCount (simulate drift)
        var h1 = await semaphore.AcquireAsync();
        var h2 = await semaphore.AcquireAsync();
        // manually add a phantom entry to exceed maxCount
        await _db.SortedSetAddAsync(key, "phantom", 0);

        // Now phantom + two real => acquiredCount == 3 > maxCount
        // GetCurrentCount should floor at 0
        Assert.That(semaphore.GetCurrentCount(), Is.EqualTo(0));
        Assert.That(await semaphore.GetCurrentCountAsync(), Is.EqualTo(0));

        // Cleanup
        await h1.DisposeAsync();
        await h2.DisposeAsync();
        await _db.KeyDeleteAsync(key);
    }
}
