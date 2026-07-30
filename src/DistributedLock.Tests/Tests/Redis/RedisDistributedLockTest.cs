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

    [Test, Category("CI")]
    public void TestDisconnectedSingleDatabaseCausesTryAcquireAsyncToThrow()
    {
        var pendingAcquire = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var database = new Mock<IDatabase>(MockBehavior.Strict);
        database
            .Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .Returns(pendingAcquire.Task);
        database
            .Setup(d => d.IsConnected(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Returns(false);

        var @lock = new RedisDistributedLock(
            "key",
            database.Object,
            options => options
                .Expiry(TimeSpan.FromMilliseconds(200))
                .MinValidityTime(TimeSpan.FromMilliseconds(100))
        );

        Assert.ThrowsAsync<RedisException>(() => @lock.TryAcquireAsync().AsTask());
    }

    [Test, Category("CI")]
    public void TestSyntheticDisconnectedFaultDoesNotMaskRealFault()
    {
        var expectedException = new TimeZoneNotFoundException();
        var faultedDatabase = new Mock<IDatabase>(MockBehavior.Strict);
        faultedDatabase
            .Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .Returns(Task.FromException<bool>(expectedException));
        faultedDatabase
            .Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(false));

        var connectedPendingAcquire = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var connectedDatabase = new Mock<IDatabase>(MockBehavior.Strict);
        connectedDatabase
            .Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .Returns(connectedPendingAcquire.Task);
        connectedDatabase
            .Setup(d => d.IsConnected(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Returns(true);

        var disconnectedPendingAcquire = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var disconnectedDatabase = new Mock<IDatabase>(MockBehavior.Strict);
        disconnectedDatabase
            .Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .Returns(disconnectedPendingAcquire.Task);
        disconnectedDatabase
            .Setup(d => d.IsConnected(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Returns(false);

        var @lock = new RedisDistributedLock(
            "key",
            new[] { faultedDatabase.Object, connectedDatabase.Object, disconnectedDatabase.Object }
        );

        Assert.ThrowsAsync<TimeZoneNotFoundException>(() => @lock.TryAcquireAsync().AsTask());
    }

    [Test, Category("CI")]
    public async Task TestSingleDatabaseContentionCausesTryAcquireAsyncToReturnNull()
    {
        var database = new Mock<IDatabase>(MockBehavior.Strict);
        database
            .Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        var @lock = new RedisDistributedLock("key", database.Object);

        Assert.That(await @lock.TryAcquireAsync(), Is.Null);
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
}
