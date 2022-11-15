using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading.Internal;
using Medallion.Threading.ZooKeeper;
using NUnit.Framework;
using org.apache.zookeeper;
using org.apache.zookeeper.data;

namespace Medallion.Threading.Tests.ZooKeeper;

public class ZooKeeperDistributedLockTest
{
    [Test, Category("CI")]
    public void TestValidatesConstructorArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedLock(null!, ZooKeeperPorts.DefaultConnectionString));
        Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedLock("name", null!));
        Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedLock(default(ZooKeeperPath), ZooKeeperPorts.DefaultConnectionString));
        Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedLock(new ZooKeeperPath("/name"), null!));
        Assert.Throws<ArgumentException>(() => new ZooKeeperDistributedLock(ZooKeeperPath.Root, ZooKeeperPorts.DefaultConnectionString));
        Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedLock(default(ZooKeeperPath), "name", ZooKeeperPorts.DefaultConnectionString));
        Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedLock(new ZooKeeperPath("/dir"), null!, ZooKeeperPorts.DefaultConnectionString));
        Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedLock(new ZooKeeperPath("/dir"), "name", default(string)!));
    }

    [Test, Category("CI")]
    public void TestNameReturnsPathString()
    {
        var @lock = new ZooKeeperDistributedLock("some/crazy/name", ZooKeeperPorts.DefaultConnectionString);
        @lock.As<IDistributedLock>().Name.ShouldEqual(@lock.Path.ToString());
    }

    [Test, Category("CI")]
    public void TestProperlyCombinesDirectoryAndName()
    {
        new ZooKeeperDistributedLock(new ZooKeeperPath("/dir"), "a", ZooKeeperPorts.DefaultConnectionString).Path.ToString().ShouldEqual("/dir/a");
        Assert.That(new ZooKeeperDistributedLock(new ZooKeeperPath("/a/b"), "c/d", ZooKeeperPorts.DefaultConnectionString).Path.ToString(), Does.StartWith("/a/b/"));
    }
}
