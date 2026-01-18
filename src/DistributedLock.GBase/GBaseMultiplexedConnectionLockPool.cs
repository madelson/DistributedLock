using Medallion.Threading.Internal.Data;

namespace Medallion.Threading.GBase;

internal static class GBaseMultiplexedConnectionLockPool
{
    public static readonly MultiplexedConnectionLockPool Instance = new(s => new GBaseDatabaseConnection(s));
}
