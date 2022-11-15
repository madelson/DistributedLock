using System;
using System.Collections.Generic;
using System.Text;
using Medallion.Threading.SqlServer;
using Medallion.Threading.Tests.Data;

namespace Medallion.Threading.Tests.SqlServer;

public sealed class TestingSqlDistributedLockProvider<TStrategy, TDb> : TestingLockProvider<TStrategy>
    where TStrategy : TestingDbSynchronizationStrategy<TDb>, new()
    where TDb : TestingDb, ITestingSqlServerDb, new()
{
    public override IDistributedLock CreateLockWithExactName(string name) =>
        this.Strategy.GetConnectionOptions()
            .Create(
                (connectionString, options) => new SqlDistributedLock(name, connectionString, ToSqlOptions(options), exactName: true),
                connection => new SqlDistributedLock(name, connection, exactName: true),
                transaction => new SqlDistributedLock(name, transaction, exactName: true));

    public override string GetSafeName(string name) => SqlDistributedLock.GetSafeName(name);

    internal static Action<SqlConnectionOptionsBuilder> ToSqlOptions((bool useMultiplexing, bool useTransaction, TimeSpan? keepaliveCadence) options) => o =>
    {
        o.UseMultiplexing(options.useMultiplexing).UseTransaction(options.useTransaction);
        if (options.keepaliveCadence is { } keepaliveCadence) { o.KeepaliveCadence(keepaliveCadence); }
    };
}

public sealed class TestingSqlDistributedReaderWriterLockProvider<TStrategy, TDb> : TestingUpgradeableReaderWriterLockProvider<TStrategy>
    where TStrategy : TestingDbSynchronizationStrategy<TDb>, new()
    where TDb : TestingDb, ITestingSqlServerDb, new()
{
    public override IDistributedUpgradeableReaderWriterLock CreateUpgradeableReaderWriterLockWithExactName(string name) =>
        this.Strategy.GetConnectionOptions()
            .Create(
                (connectionString, options) =>
                    new SqlDistributedReaderWriterLock(name, connectionString, TestingSqlDistributedLockProvider<TStrategy, TDb>.ToSqlOptions(options), exactName: true),
                connection => new SqlDistributedReaderWriterLock(name, connection, exactName: true),
                transaction => new SqlDistributedReaderWriterLock(name, transaction, exactName: true));

    public override string GetSafeName(string name) => SqlDistributedReaderWriterLock.GetSafeName(name);
}

public sealed class TestingSqlDistributedSemaphoreProvider<TStrategy, TDb> : TestingSemaphoreProvider<TStrategy>
    where TStrategy : TestingDbSynchronizationStrategy<TDb>, new()
    where TDb : TestingDb, ITestingSqlServerDb, new()
{
    public override IDistributedSemaphore CreateSemaphoreWithExactName(string name, int maxCount) =>
        this.Strategy.GetConnectionOptions()
            .Create(
                (connectionString, options) =>
                    new SqlDistributedSemaphore(name, maxCount, connectionString, TestingSqlDistributedLockProvider<TStrategy, TDb>.ToSqlOptions(options)),
                connection => new SqlDistributedSemaphore(name, maxCount, connection),
                transaction => new SqlDistributedSemaphore(name, maxCount, transaction));

    public override string GetSafeName(string name) => name ?? throw new ArgumentNullException(nameof(name));
}
