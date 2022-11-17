using Medallion.Threading.ZooKeeper;

namespace Medallion.Threading.Tests.ZooKeeper;

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

    public override string GetSafeName(string name) => new ZooKeeperDistributedReaderWriterLock(name, ZooKeeperPorts.DefaultConnectionString).Path.ToString();
}

public sealed class TestingZooKeeperDistributedSemaphoreProvider : TestingSemaphoreProvider<TestingZooKeeperSynchronizationStrategy>
{
    public override IDistributedSemaphore CreateSemaphoreWithExactName(string name, int maxCount)
    {
        var semaphore = new ZooKeeperDistributedSemaphore(new ZooKeeperPath(name), maxCount, ZooKeeperPorts.DefaultConnectionString, this.Strategy.AssumeNodeExists, this.Strategy.Options);
        this.Strategy.TrackPath(name);
        return semaphore;
    }

    public override string GetSafeName(string name) => new ZooKeeperDistributedSemaphore(name, maxCount: 1, ZooKeeperPorts.DefaultConnectionString).Path.ToString();
}
