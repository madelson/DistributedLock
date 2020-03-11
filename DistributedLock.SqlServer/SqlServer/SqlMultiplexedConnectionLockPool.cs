using Medallion.Threading.Internal.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Medallion.Threading.SqlServer
{
    internal static class SqlMultiplexedConnectionLockPool
    {
        public static readonly MultiplexedConnectionLockPool Instance =
            // todo how should multiplexing get the keepalive timeout? Ideally this would be something changeable on the connection.
            // The multiplexed lock would keep a sorteddict<timeout, count> for all held locks and use the min timeout as the current timeout for
            // the connection
            new MultiplexedConnectionLockPool(s => new SqlDatabaseConnection(s, Timeout.InfiniteTimeSpan));
    }
}
