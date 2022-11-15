using Medallion.Threading.MySql;
using Medallion.Threading.Tests.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests.MySql;

public sealed class TestingMySqlDistributedLockProvider<TStrategy, TDb> : TestingLockProvider<TStrategy>
    where TStrategy : TestingDbSynchronizationStrategy<TDb>, new()
    where TDb : TestingMySqlDb, new()
{
    public override IDistributedLock CreateLockWithExactName(string name) =>
        this.Strategy.GetConnectionOptions()
            .Create(
                (connectionString, options) => new MySqlDistributedLock(name, connectionString, options: ToMySqlOptions(options)),
                connection => new MySqlDistributedLock(name, connection, exactName: true),
                transaction => new MySqlDistributedLock(name, transaction, exactName: true)
        );

    public override string GetSafeName(string name) => new MySqlDistributedLock(name, new TDb().ConnectionStringBuilder.ConnectionString).Name;

    public override string GetCrossProcessLockType() => (typeof(TDb) == typeof(TestingMariaDbDb) ? "MariaDB" : string.Empty) + base.GetCrossProcessLockType();

    internal static Action<MySqlConnectionOptionsBuilder> ToMySqlOptions((bool useMultiplexing, bool useTransaction, TimeSpan? keepaliveCadence) options) => o =>
    {
        o.UseMultiplexing(options.useMultiplexing);
        if (options.keepaliveCadence is { } keepaliveCadence) { o.KeepaliveCadence(keepaliveCadence); }
    };
}
