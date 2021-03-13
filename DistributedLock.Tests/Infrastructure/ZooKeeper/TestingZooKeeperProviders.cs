using Medallion.Threading.ZooKeeper;
using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests.ZooKeeper
{
    public sealed class TestingZooKeeperDistributedLockProvider : TestingLockProvider<TestingZooKeeperSynchronizationStrategy>
    {
        public override IDistributedLock CreateLockWithExactName(string name)
        {
            var @lock = new ZooKeeperDistributedLock(name.TrimStart(ZooKeeperPath.Separator), ZooKeeperPorts.DefaultConnectionString);
            @lock.Path.ToString().ShouldEqual(name); // sanity check
            this.Strategy.TrackPath(@lock.Path.ToString());
            return @lock;
        }

        public override string GetSafeName(string name) => new ZooKeeperDistributedLock(name, ZooKeeperPorts.DefaultConnectionString).Path.ToString();
    }
}
