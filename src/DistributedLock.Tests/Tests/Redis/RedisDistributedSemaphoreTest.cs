using Medallion.Threading.Redis;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;

namespace Medallion.Threading.Tests.Redis;

public class RedisDistributedSemaphoreTest
{
    [Test]
    [Category("CI")]
    public void TestName()
    {
        const string Name = "\0🐉汉字\b\r\n\\";
        var @lock = new RedisDistributedSemaphore(Name, 1, new Mock<IDatabase>(MockBehavior.Strict).Object);
        @lock.Name.ShouldEqual(Name);
    }

    [Test]
    [Category("CI")]
    public void TestValidatesConstructorParameters()
    {
        var database = new Mock<IDatabase>(MockBehavior.Strict).Object;
        Assert.Throws<ArgumentNullException>(() => new RedisDistributedSemaphore(default!, 2, database));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RedisDistributedSemaphore("key", 0, database));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RedisDistributedSemaphore("key", -1, database));
        Assert.Throws<ArgumentNullException>(() => new RedisDistributedSemaphore("key", 2, default(IDatabase)!));
    }

    [Test]
    [Category("CI")]
    public async Task TestGetCurrentCountAsync()
    {
        const int maxCount = 5;
        const int heldCount = 2;
        var expectedAvailable = maxCount - heldCount;

        var databaseMock = new Mock<IDatabase>(MockBehavior.Strict);
        databaseMock
              .Setup(db => db.SortedSetLengthAsync(
                  It.Is<RedisKey>(k => k == "test-key"),
                  It.IsAny<double>(),
                  It.IsAny<double>(),
                  It.IsAny<Exclude>(),
                  CommandFlags.DemandMaster))
              .ReturnsAsync(heldCount);

        var semaphore = new RedisDistributedSemaphore("test-key", 5, databaseMock.Object);
        var available = await semaphore.GetCurrentCountAsync();

        available.ShouldEqual(expectedAvailable);

        databaseMock.VerifyAll();
    }

    [Test]
    [Category("CI")]
    public void TestGetCurrentCountSync()
    {
        const int maxCount = 4;
        const int heldCount = 3;
        var expectedAvailable = maxCount - heldCount;

        var databaseMock = new Mock<IDatabase>(MockBehavior.Strict);
        databaseMock
             .Setup(db => db.SortedSetLength(
                 It.Is<RedisKey>(k => k == "test-key"),
                 It.IsAny<double>(),
                 It.IsAny<double>(),
                 It.IsAny<Exclude>(),
                 CommandFlags.DemandMaster))
             .Returns(heldCount);

        var semaphore = new RedisDistributedSemaphore("test-key", maxCount, databaseMock.Object);
        var available = semaphore.GetCurrentCount();

        expectedAvailable.ShouldEqual(available);
        databaseMock.VerifyAll();
    }


    [Test]
    public async Task TestGetCurrentCountReflectsAcquisitionsAndReleases()
    {
        var db = RedisServer.GetDefaultServer(0).Multiplexer.GetDatabase();

        const int maxCount = 3;
        var key = TestHelper.UniqueName + ":sem";
        var semaphore = new RedisDistributedSemaphore(key, maxCount, db);

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
        var db = RedisServer.GetDefaultServer(0).Multiplexer.GetDatabase();

        const int maxCount = 2;
        var key = TestHelper.UniqueName + ":sem2";
        var semaphore = new RedisDistributedSemaphore(key, maxCount, db);

        // Acquire more than maxCount (simulate drift)
        var h1 = await semaphore.AcquireAsync();
        var h2 = await semaphore.AcquireAsync();
        // manually add a phantom entry to exceed maxCount
        await db.SortedSetAddAsync(key, "phantom", 0);

        // Now phantom + two real => acquiredCount == 3 > maxCount
        // GetCurrentCount should floor at 0
        Assert.That(semaphore.GetCurrentCount(), Is.EqualTo(0));
        Assert.That(await semaphore.GetCurrentCountAsync(), Is.EqualTo(0));

        // Cleanup
        await h1.DisposeAsync();
        await h2.DisposeAsync();
        await db.KeyDeleteAsync(key);
    }
}
