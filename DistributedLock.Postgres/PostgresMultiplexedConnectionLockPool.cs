using Medallion.Threading.Internal.Data;

namespace Medallion.Threading.Postgres;

internal static class PostgresMultiplexedConnectionLockPool
{
    public static readonly MultiplexedConnectionLockPool Instance = new(s => new PostgresDatabaseConnection(s));
}
