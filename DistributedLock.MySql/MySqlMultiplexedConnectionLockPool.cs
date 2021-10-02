using Medallion.Threading.Internal.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.MySql
{
    internal static class MySqlMultiplexedConnectionLockPool
    {
        public static readonly MultiplexedConnectionLockPool Instance = new(s => new MySqlDatabaseConnection(s));
    }
}
