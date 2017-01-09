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
        /// Tries to "optimistically" re-use a single shared connection for all active locks held on the same
        /// connection string. This is done in a way so that locking operations never block trying to acquire
        /// access to the shared connection or waiting to acquire the lock on the shared connection. Instead, we
        /// are simply "optimistic" in hoping that both the shared connection and the lock are available immediately,
        /// which will be true in many applications. If the shared connection cannot be used, this strategy falls
        /// back to <see cref="Connection"/>.
        /// 
        /// This option can improve performance and avoid connection pool starvation in high-load scenarios. It is also
        /// particularly applicable to cases where <see cref="SqlDistributedLock.TryAcquire(TimeSpan, System.Threading.CancellationToken)"/>
        /// semantics are used with a zero-length timeout.
        /// </summary>
        OptimisticConnectionPooling = 3,
    }
}
