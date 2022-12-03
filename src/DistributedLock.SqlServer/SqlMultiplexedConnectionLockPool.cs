using Medallion.Threading.Internal.Data;

namespace Medallion.Threading.SqlServer;

internal static class SqlMultiplexedConnectionLockPool
{
    public static readonly MultiplexedConnectionLockPool Instance = new(s => new SqlDatabaseConnection(s));
}
