using Medallion.Threading.Internal.Data;

namespace Medallion.Threading.SqlServer;

internal static class SqlMultiplexedConnectionLockPool
{
    public static readonly MultiplexedConnectionLockPool<string> Instance = new(s => new SqlDatabaseConnection(s));
}

#if NET7_0_OR_GREATER
internal static class SqlDataSourceMultiplexedConnectionLockPool
{
    public static readonly MultiplexedConnectionLockPool<System.Data.Common.DbDataSource> Instance =
        new(dataSource => new SqlDatabaseConnection(dataSource), ReferenceEqualityComparer.Instance);
}
#endif