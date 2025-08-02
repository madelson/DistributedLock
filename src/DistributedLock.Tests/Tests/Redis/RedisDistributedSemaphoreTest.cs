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
        // Arrange
        const int maxCount = 5;
        const int acquiredCount = 2;
        var expectedAvailable = maxCount - acquiredCount;

        var databaseMock = new Mock<IDatabase>(MockBehavior.Strict);

        // Mock ScriptEvaluateAsync to return acquiredCount
        databaseMock
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisResult)(object)(long)acquiredCount);

        var semaphore = new RedisDistributedSemaphore("test-key", maxCount, databaseMock.Object);

        // Act
        var availableCount = await semaphore.GetCurrentCountAsync();

        // Assert
        availableCount.ShouldEqual(expectedAvailable);

        databaseMock.VerifyAll();
    }

}
