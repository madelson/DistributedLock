using Medallion.Threading.Internal;
using Medallion.Threading.ZooKeeper;
using NUnit.Framework;

namespace Medallion.Threading.Tests.ZooKeeper;

public class ZooKeeperDistributedSemaphoreTest
{
    [Test, Category("CI")]
    public void TestValidatesConstructorArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedSemaphore(null!, 2, ZooKeeperPorts.DefaultConnectionString));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ZooKeeperDistributedSemaphore("name", -1, ZooKeeperPorts.DefaultConnectionString));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ZooKeeperDistributedSemaphore("name", 0, ZooKeeperPorts.DefaultConnectionString));
        Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedSemaphore("name", 2, null!));
        Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedSemaphore(default(ZooKeeperPath), 2, ZooKeeperPorts.DefaultConnectionString));
        Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedSemaphore(new ZooKeeperPath("/name"), 2, null!));
        Assert.Throws<ArgumentException>(() => new ZooKeeperDistributedSemaphore(ZooKeeperPath.Root, 2, ZooKeeperPorts.DefaultConnectionString));
        Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedSemaphore(default(ZooKeeperPath), "name", 2, ZooKeeperPorts.DefaultConnectionString));
        Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedSemaphore(new ZooKeeperPath("/dir"), null!, 2, ZooKeeperPorts.DefaultConnectionString));
        Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedSemaphore(new ZooKeeperPath("/dir"), "name", 2, default(string)!));
    }

    [Test, Category("CI")]
    public void TestNameReturnsPathString()
    {
        var @lock = new ZooKeeperDistributedSemaphore("some/crazy/name", 2, ZooKeeperPorts.DefaultConnectionString);
        @lock.As<IDistributedSemaphore>().Name.ShouldEqual(@lock.Path.ToString());
    }

    [Test, Category("CI")]
    public void TestProperlyCombinesDirectoryAndName()
    {
        new ZooKeeperDistributedSemaphore(new ZooKeeperPath("/dir"), "a", 2, ZooKeeperPorts.DefaultConnectionString).Path.ToString().ShouldEqual("/dir/a");
        Assert.That(new ZooKeeperDistributedSemaphore(new ZooKeeperPath("/a/b"), "c/d", 2, ZooKeeperPorts.DefaultConnectionString).Path.ToString(), Does.StartWith("/a/b/"));
    }
}
