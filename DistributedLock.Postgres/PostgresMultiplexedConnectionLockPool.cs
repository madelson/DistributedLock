using Medallion.Threading.Internal.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Medallion.Threading.Postgres
{
    internal static class PostgresMultiplexedConnectionLockPool
    {
        public static readonly MultiplexedConnectionLockPool Instance = new(s => new PostgresDatabaseConnection(s));
    }
}
