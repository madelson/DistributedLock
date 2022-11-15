using Medallion.Threading.Internal.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Oracle;

internal static class OracleMultiplexedConnectionLockPool
{
    public static readonly MultiplexedConnectionLockPool Instance = new(s => new OracleDatabaseConnection(s));
}
