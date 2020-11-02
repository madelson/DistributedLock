using Medallion.Threading.Redis;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Medallion.Threading.Tests.Redis
{
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
}
