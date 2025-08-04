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
}
