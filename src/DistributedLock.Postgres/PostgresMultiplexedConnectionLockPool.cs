using Medallion.Threading.Internal.Data;
#if NET7_0_OR_GREATER
using System.Data.Common;
#endif

namespace Medallion.Threading.Postgres;

internal static class PostgresMultiplexedConnectionLockPool
{
    public static readonly MultiplexedConnectionLockPool<string> Instance = new(s => new PostgresDatabaseConnection(s));

#if NET7_0_OR_GREATER
    /// <summary>
    /// Pooled by <see cref="DbDataSource"/> instance: two data sources with the same connection string do not share connections
    /// </summary>
    public static readonly MultiplexedConnectionLockPool<DbDataSource> DataSourceInstance =
        new(dataSource => new PostgresDatabaseConnection(dataSource), ReferenceEqualityComparer.Instance);
#endif
}
