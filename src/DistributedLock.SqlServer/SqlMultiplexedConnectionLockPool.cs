using Medallion.Threading.Internal.Data;

namespace Medallion.Threading.SqlServer;

internal static class SqlMultiplexedConnectionLockPool
{
    public static readonly MultiplexedConnectionLockPool<string> Instance = new(s => new SqlDatabaseConnection(s));
}
