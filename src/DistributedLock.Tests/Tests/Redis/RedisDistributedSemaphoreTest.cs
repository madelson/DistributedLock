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
        const int acquiredCount = 2;
        var expectedAvailable = maxCount - acquiredCount;
        var scriptSubstring = "zcard";

        var databaseMock = new Mock<IDatabase>(MockBehavior.Strict);

        var fakeResult = RedisResult.Create((RedisValue)(long)acquiredCount);
        var fakeTask = Task.FromResult(fakeResult);

        databaseMock
            .Setup(db => db.ScriptEvaluateAsync(
                It.Is<string>(s => s.Contains(scriptSubstring)),
                It.Is<RedisKey[]>(keys => keys.Length == 1 && keys[0] == "test-key"),
                null,
                CommandFlags.None))
            .Returns(fakeTask);

        var semaphore = new RedisDistributedSemaphore("test-key", maxCount, databaseMock.Object);
        var available = await semaphore.GetCurrentCountAsync();

        expectedAvailable.ShouldEqual(available);

        databaseMock.VerifyAll();
    }

    [Test]
    [Category("CI")]
    public void TestGetCurrentCountSync()
    {
        const int maxCount = 5;
        const int acquiredCount = 3;
        var expectedAvailable = maxCount - acquiredCount;
        var fakeResult = RedisResult.Create((RedisValue)(long)acquiredCount);
        var fakeTask = Task.FromResult(fakeResult);

        var databaseMock = new Mock<IDatabase>(MockBehavior.Strict);
        databaseMock
            .Setup(db => db.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                null,
                CommandFlags.None))
            .Returns(fakeTask);

        var semaphore = new RedisDistributedSemaphore("test-key", maxCount, databaseMock.Object);
        var available = semaphore.GetCurrentCount();

        expectedAvailable.ShouldEqual(available);
        databaseMock.VerifyAll();
    }
}
