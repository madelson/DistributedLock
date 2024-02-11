using Medallion.Threading.Internal.Data;

namespace Medallion.Threading.Oracle;

internal static class OracleMultiplexedConnectionLockPool
{
    public static readonly MultiplexedConnectionLockPool Instance = new(s => new OracleDatabaseConnection(s));
}
