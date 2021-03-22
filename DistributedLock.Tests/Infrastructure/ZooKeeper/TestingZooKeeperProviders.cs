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
            var @lock = new ZooKeeperDistributedLock(new ZooKeeperPath(name), ZooKeeperPorts.DefaultConnectionString, this.Strategy.AssumeNodeExists, this.Strategy.Options);
            this.Strategy.TrackPath(name);
            return @lock;
        }

        public override string GetSafeName(string name) => new ZooKeeperDistributedLock(name, ZooKeeperPorts.DefaultConnectionString).Path.ToString();
    }

    public sealed class TestingZooKeeperDistributedReaderWriterLockProvider : TestingReaderWriterLockProvider<TestingZooKeeperSynchronizationStrategy>
    {
        public override IDistributedReaderWriterLock CreateReaderWriterLockWithExactName(string name)
        {
            var @lock = new ZooKeeperDistributedReaderWriterLock(new ZooKeeperPath(name), ZooKeeperPorts.DefaultConnectionString, this.Strategy.AssumeNodeExists, this.Strategy.Options);
            this.Strategy.TrackPath(name);
            return @lock;
        }

        public override string GetSafeName(string name) => new ZooKeeperDistributedLock(name, ZooKeeperPorts.DefaultConnectionString).Path.ToString();
    }
}
