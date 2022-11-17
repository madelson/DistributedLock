using Medallion.Threading.Redis;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;

namespace Medallion.Threading.Tests.Redis;

[Category("CI")]
public class RedisDistributedLockTest
{
    [Test]
    public void TestName()
    {
        const string Name = "\0🐉汉字\b\r\n\\";
        var @lock = new RedisDistributedLock(Name, new Mock<IDatabase>(MockBehavior.Strict).Object);
        @lock.Name.ShouldEqual(Name);
        @lock.Key.ShouldEqual(new RedisKey(Name));
    }

    [Test]
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
}
