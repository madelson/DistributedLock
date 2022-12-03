using Medallion.Threading.Internal.Data;

namespace Medallion.Threading.MySql;

internal static class MySqlMultiplexedConnectionLockPool
{
    public static readonly MultiplexedConnectionLockPool Instance = new(s => new MySqlDatabaseConnection(s));
}
