using Medallion.Threading.Redis;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;
using System.Globalization;
using Testcontainers.Redis;

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
                RedisServer.CreateDatabase(RedisSetUpFixture.Redis)
            );
            await (await @lock.AcquireAsync()).DisposeAsync();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCultureInfo;
        }
    }
}
