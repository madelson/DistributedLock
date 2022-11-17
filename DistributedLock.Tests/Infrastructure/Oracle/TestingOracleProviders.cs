using Medallion.Threading.Oracle;
using Medallion.Threading.Tests.Data;

namespace Medallion.Threading.Tests.Oracle;

public sealed class TestingOracleDistributedLockProvider<TStrategy> : TestingLockProvider<TStrategy>
    where TStrategy : TestingDbSynchronizationStrategy<TestingOracleDb>, new()
{
    public override IDistributedLock CreateLockWithExactName(string name) =>
        this.Strategy.GetConnectionOptions()
            .Create(
                (connectionString, options) => new OracleDistributedLock(name, connectionString, options: ToOracleOptions(options)),
                connection => new OracleDistributedLock(name, connection),
                transaction => new OracleDistributedLock(name, transaction.Connection)
            );

    public override string GetSafeName(string name) => new OracleDistributedLock(name, TestingOracleDb.DefaultConnectionString).Name;

    internal static Action<OracleConnectionOptionsBuilder> ToOracleOptions((bool useMultiplexing, bool useTransaction, TimeSpan? keepaliveCadence) options) => o =>
    {
        o.UseMultiplexing(options.useMultiplexing);
        if (options.keepaliveCadence is { } keepaliveCadence) { o.KeepaliveCadence(keepaliveCadence); }
    };
}

public sealed class TestingOracleDistributedReaderWriterLockProvider<TStrategy> : TestingUpgradeableReaderWriterLockProvider<TStrategy>
    where TStrategy : TestingDbSynchronizationStrategy<TestingOracleDb>, new()
{
    public override IDistributedUpgradeableReaderWriterLock CreateUpgradeableReaderWriterLockWithExactName(string name) =>
        this.Strategy.GetConnectionOptions()
            .Create(
                (connectionString, options) =>
                    new OracleDistributedReaderWriterLock(name, connectionString, TestingOracleDistributedLockProvider<TStrategy>.ToOracleOptions(options), exactName: true),
                connection => new OracleDistributedReaderWriterLock(name, connection, exactName: true),
                transaction => new OracleDistributedReaderWriterLock(name, transaction.Connection, exactName: true));

    public override string GetSafeName(string name) => new OracleDistributedReaderWriterLock(name, TestingOracleDb.DefaultConnectionString).Name;
}
