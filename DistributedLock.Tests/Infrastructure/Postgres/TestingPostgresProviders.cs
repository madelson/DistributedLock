using Medallion.Threading.Postgres;
using Medallion.Threading.Tests.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests.Postgres
{
    public sealed class TestingPostgresDistributedLockProvider<TStrategy> : TestingLockProvider<TStrategy>
        where TStrategy : TestingDbSynchronizationStrategy<TestingPostgresDb>, new()
    {
        public override IDistributedLock CreateLockWithExactName(string name) =>
            this.Strategy.GetConnectionOptions()
                .Create(
                    (connectionString, options) => new PostgresDistributedLock(
                        new PostgresAdvisoryLockKey(name, allowHashing: false), 
                        connectionString, 
                        ToPostgresOptions(options)
                    ),
                    connection => new PostgresDistributedLock(new PostgresAdvisoryLockKey(name, allowHashing: false), connection),
                    transaction => new PostgresDistributedLock(new PostgresAdvisoryLockKey(name, allowHashing: false), transaction.Connection)
            );

        public override string GetSafeName(string name) => PostgresDistributedLock.GetSafeName(name).ToString();

        internal static Action<PostgresConnectionOptionsBuilder> ToPostgresOptions((bool useMultiplexing, bool useTransaction, TimeSpan keepaliveCadence) options) =>
            o => o.UseMultiplexing(options.useMultiplexing).KeepaliveCadence(options.keepaliveCadence);
    }

    public sealed class TestingPostgresDistributedReaderWriterLockProvider<TStrategy> : TestingReaderWriterLockProvider<TStrategy>
        where TStrategy : TestingDbSynchronizationStrategy<TestingPostgresDb>, new()
    {
        public override IDistributedReaderWriterLock CreateReaderWriterLockWithExactName(string name) =>
            this.Strategy.GetConnectionOptions()
                .Create(
                    (connectionString, options) =>
                        new PostgresDistributedReaderWriterLock(
                            new PostgresAdvisoryLockKey(name, allowHashing: false), 
                            connectionString, 
                            TestingPostgresDistributedLockProvider<TStrategy>.ToPostgresOptions(options)
                        ),
                    connection => new PostgresDistributedReaderWriterLock(new PostgresAdvisoryLockKey(name, allowHashing: false), connection),
                    transaction => new PostgresDistributedReaderWriterLock(new PostgresAdvisoryLockKey(name, allowHashing: false), transaction.Connection)
                );

        public override string GetSafeName(string name) => PostgresDistributedReaderWriterLock.GetSafeName(name).ToString();
    }
}
