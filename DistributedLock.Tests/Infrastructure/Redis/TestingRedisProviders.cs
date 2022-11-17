using Medallion.Threading.Redis;

namespace Medallion.Threading.Tests.Redis;

public sealed class TestingRedisDistributedLockProvider<TDatabaseProvider> : TestingLockProvider<TestingRedisSynchronizationStrategy<TDatabaseProvider>>
    where TDatabaseProvider : TestingRedisDatabaseProvider, new()
{
    public override IDistributedLock CreateLockWithExactName(string name)
    {
        var @lock = new RedisDistributedLock(name, this.Strategy.DatabaseProvider.Databases, this.Strategy.Options);
        this.Strategy.RegisterKillHandleAction(
            () => this.Strategy.DatabaseProvider.Databases.Take((this.Strategy.DatabaseProvider.Databases.Count / 2) + 1)
                .ToList()
                .ForEach(db => db.KeyDelete(@lock.Key))
        );
        return @lock;
    }

    public override string GetSafeName(string name) => new RedisDistributedLock(name, this.Strategy.DatabaseProvider.Databases).Name;

    public override string GetCrossProcessLockType() => $"{nameof(RedisDistributedLock)}{this.Strategy.DatabaseProvider.CrossProcessLockTypeSuffix}";
}

public sealed class TestingRedisDistributedReaderWriterLockProvider<TDatabaseProvider> : TestingReaderWriterLockProvider<TestingRedisSynchronizationStrategy<TDatabaseProvider>>
    where TDatabaseProvider : TestingRedisDatabaseProvider, new()
{
    public override IDistributedReaderWriterLock CreateReaderWriterLockWithExactName(string name)
    {
        var @lock = new RedisDistributedReaderWriterLock(name, this.Strategy.DatabaseProvider.Databases, this.Strategy.Options);
        this.Strategy.RegisterKillHandleAction(
            () => this.Strategy.DatabaseProvider.Databases.Take((this.Strategy.DatabaseProvider.Databases.Count / 2) + 1)
                .ToList()
                .ForEach(db => 
                {
                    db.KeyDelete(@lock.ReaderKey);
                    db.KeyDelete(@lock.WriterKey);
                })
        );
        return @lock;
    }

    public override string GetSafeName(string name) => new RedisDistributedReaderWriterLock(name, this.Strategy.DatabaseProvider.Databases).Name;

    public override string GetCrossProcessLockType(ReaderWriterLockType type) => $"{type}{nameof(RedisDistributedReaderWriterLock)}{this.Strategy.DatabaseProvider.CrossProcessLockTypeSuffix}";
}

public sealed class TestingRedisDistributedSemaphoreProvider : TestingSemaphoreProvider<TestingRedisSynchronizationStrategy<TestingRedisSingleDatabaseProvider>>
{
    public override IDistributedSemaphore CreateSemaphoreWithExactName(string name, int maxCount)
    {
        var semaphore = new RedisDistributedSemaphore(name, maxCount, this.Strategy.DatabaseProvider.Databases.Single(), this.Strategy.Options);
        this.Strategy.RegisterKillHandleAction(
            () => this.Strategy.DatabaseProvider.Databases.Take((this.Strategy.DatabaseProvider.Databases.Count / 2) + 1)
                .ToList()
                .ForEach(db => db.KeyDelete(semaphore.Key))
        );
        return semaphore;
    }

    public override string GetSafeName(string name) => new RedisDistributedSemaphore(name, 1, this.Strategy.DatabaseProvider.Databases.Single()).Name;
}
