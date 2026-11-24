using Medallion.Threading.Internal.Data;

namespace Medallion.Threading.Postgres;

internal static class PostgresMultiplexedConnectionLockPool
{
    public static readonly MultiplexedConnectionLockPool<string> Instance = new(s => new PostgresDatabaseConnection(s));    
}

#if NET7_0_OR_GREATER
internal static class PostgresDataSourceMultiplexedConnectionLockPool
{
    public static readonly MultiplexedConnectionLockPool<System.Data.Common.DbDataSource> Instance =
        new(dataSource => new PostgresDatabaseConnection(dataSource), ReferenceEqualityComparer.Instance);
}
#endif
