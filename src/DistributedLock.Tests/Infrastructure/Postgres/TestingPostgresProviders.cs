using Medallion.Threading.Postgres;
using Medallion.Threading.Tests.Data;

namespace Medallion.Threading.Tests.Postgres;

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
                transaction => new PostgresDistributedLock(new PostgresAdvisoryLockKey(name, allowHashing: false), transaction.Connection!)
        );

    public override string GetSafeName(string name) => new PostgresAdvisoryLockKey(name, allowHashing: true).ToString();

    internal static Action<PostgresConnectionOptionsBuilder> ToPostgresOptions((bool useMultiplexing, bool useTransaction, TimeSpan? keepaliveCadence) options) => o =>
    {
        o.UseMultiplexing(options.useMultiplexing);
        o.UseTransaction(options.useTransaction);
        if (options.keepaliveCadence is { } keepaliveCadence) { o.KeepaliveCadence(keepaliveCadence); }
    };
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
                transaction => new PostgresDistributedReaderWriterLock(new PostgresAdvisoryLockKey(name, allowHashing: false), transaction.Connection!)
            );

    public override string GetSafeName(string name) => new PostgresAdvisoryLockKey(name, allowHashing: true).ToString();
}
