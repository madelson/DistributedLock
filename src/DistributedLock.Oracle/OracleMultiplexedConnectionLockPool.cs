using Medallion.Threading.Internal.Data;

namespace Medallion.Threading.Oracle;

internal static class OracleMultiplexedConnectionLockPool
{
    public static readonly MultiplexedConnectionLockPool<string> Instance = new(s => new OracleDatabaseConnection(s));
}

#if NET7_0_OR_GREATER
internal static class OracleDataSourceMultiplexedConnectionLockPool
{
    public static readonly MultiplexedConnectionLockPool<System.Data.Common.DbDataSource> Instance =
        new(dataSource => new OracleDatabaseConnection(dataSource), ReferenceEqualityComparer.Instance);
}
#endif