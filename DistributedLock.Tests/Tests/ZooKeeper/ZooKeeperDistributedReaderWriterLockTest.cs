using Medallion.Threading.Internal;
using Medallion.Threading.ZooKeeper;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests.ZooKeeper
{
    public class ZooKeeperDistributedReaderWriterLockTest
    {
        [Test, Category("CI")]
        public void TestValidatesConstructorArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedReaderWriterLock(null!, ZooKeeperPorts.DefaultConnectionString));
            Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedReaderWriterLock("name", null!));
            Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedReaderWriterLock(default(ZooKeeperPath), ZooKeeperPorts.DefaultConnectionString));
            Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedReaderWriterLock(new ZooKeeperPath("/name"), null!));
            Assert.Throws<ArgumentException>(() => new ZooKeeperDistributedReaderWriterLock(ZooKeeperPath.Root, ZooKeeperPorts.DefaultConnectionString));
            Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedReaderWriterLock(default(ZooKeeperPath), "name", ZooKeeperPorts.DefaultConnectionString));
            Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedReaderWriterLock(new ZooKeeperPath("/dir"), null!, ZooKeeperPorts.DefaultConnectionString));
            Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedReaderWriterLock(new ZooKeeperPath("/dir"), "name", default(string)!));
        }

        [Test, Category("CI")]
        public void TestNameReturnsPathString()
        {
            var @lock = new ZooKeeperDistributedReaderWriterLock("some/crazy/name", ZooKeeperPorts.DefaultConnectionString);
            @lock.As<IDistributedReaderWriterLock>().Name.ShouldEqual(@lock.Path.ToString());
        }

        [Test, Category("CI")]
        public void TestProperlyCombinesDirectoryAndName()
        {
            new ZooKeeperDistributedReaderWriterLock(new ZooKeeperPath("/dir"), "a", ZooKeeperPorts.DefaultConnectionString).Path.ToString().ShouldEqual("/dir/a");
            Assert.That(new ZooKeeperDistributedReaderWriterLock(new ZooKeeperPath("/a/b"), "c/d", ZooKeeperPorts.DefaultConnectionString).Path.ToString(), Does.StartWith("/a/b/"));
        }
    }
}
