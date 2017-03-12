using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    /// <summary>
    /// Determines how a <see cref="SqlDistributedLock"/> manages its connection
    /// </summary>
    public enum SqlDistributedLockConnectionStrategy
    {
        /// <summary>
        /// Specifies the default strategy. Currently, this is equivalent to <see cref="Connection"/>
        /// </summary>
        Default = 0,

        /// <summary>
        /// Uses a connection-scoped lock. This is marginally more expensive than <see cref="Transaction"/> 
        /// due to the need for an explicit sp_releaseapplock call, but has the benefit of not maintaining
        /// a potentially-long-running transaction which can be problematic for databases using the full
        /// recovery model
        /// </summary>
        Connection = 1,

        /// <summary>
        /// Uses a transaction-scoped lock. This is marginally less expensive than <see cref="Connection"/>
        /// because releasing the lock requires only disposing the underlying <see cref="SqlTransaction"/>.
        /// The disadvantage is that using this strategy may lead to long-running transactions, which can be
        /// problematic for databases using the full recovery model
        /// </summary>
        Transaction = 2,

        /// <summary>
        /// This mode takes advantage of the fact that while "holding" a lock a connection is essentially idle. Thus,
        /// rather than creating a new connection for each held lock it is often possible to multiplex a shared connection
        /// so that that connection can hold multiple locks at the same time.
        /// 
        /// This is implemented in such a way that releasing a lock held on such a connection will never be blocked by an
        /// Acquire() call that is waiting to acquire a lock on that same connection. For this reason, the multiplexing
        /// strategy is "optimistic": if the lock can't be acquired instantaneously on the shared connection, a new (shareable) 
        /// connection will be allocated.
        /// 
        /// This option can improve performance and avoid connection pool starvation in high-load scenarios. It is also
        /// particularly applicable to cases where <see cref="SqlDistributedLock.TryAcquire(TimeSpan, System.Threading.CancellationToken)"/>
        /// semantics are used with a zero-length timeout.
        /// </summary>
        OptimisticConnectionMultiplexing = 3,

        /// <summary>
        /// Using SQL Azure as a distributed lock provider can be challenging due to Azure's aggressive connection governor
        /// which proactively kills idle connections. Using this strategy, the lock attempts to account for this by issuing
        /// periodic no-op "keepalive" queries on the locking connection to prevent it from becoming idle. Note that this still
        /// does not guarantee protection for the connection from all conditions where the governor might kill it.
        /// 
        /// For more information, see the dicussion on https://github.com/madelson/DistributedLock/issues/5
        /// </summary>
        Azure = 4,
    }
}
