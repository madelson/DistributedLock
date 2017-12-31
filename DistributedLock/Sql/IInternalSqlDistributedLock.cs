using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    // todo consider rename
    /// <summary>
    /// There are several strategies for implementing SQL-based locks; this interface
    /// abstracts between them to keep the implementation of <see cref="SqlDistributedLock"/> manageable
    /// </summary>
    internal interface IInternalSqlDistributedLock
    {
        // the contextHandle argument to these methods is used when acquiring a nested lock, such as upgrading
        // from an upgradeable read lock to a write lock. This allows the implementation to use the same connection
        // for the nested lock

        IDisposable TryAcquire<TLockCookie>(int timeoutMillis, ISqlSynchronizationStrategy<TLockCookie> strategy, IDisposable contextHandle)
            where TLockCookie : class;
        Task<IDisposable> TryAcquireAsync<TLockCookie>(int timeoutMillis, ISqlSynchronizationStrategy<TLockCookie> strategy, CancellationToken cancellationToken, IDisposable contextHandle)
            where TLockCookie : class;
    }

    // todo move to own file or rename existing file
    /// <summary>
    /// Represents a "locking algorithm" implemented in SQL
    /// </summary>
    interface ISqlSynchronizationStrategy<TLockCookie>
        where TLockCookie : class
    {
        /// <summary>
        /// True iff the lock taken by the algorithm can be upgraded on the same connection (basically for upgradeable read locks).
        /// 
        /// We need this property because the multiplexing approach has to avoid multiplexing upgradeable locks since they may block
        /// indefinitely on the held connection (which would prevent other locks on that connection from releasing) during an upgrade
        /// operation.
        /// </summary>
        bool IsUpgradeable { get; }

        /// <summary>
        /// Attempts to acquire the lock, returning either null for failure or a non-null state "cookie" on success
        /// </summary>
        TLockCookie TryAcquire(ConnectionOrTransaction connectionOrTransaction, string resourceName, int timeoutMillis);

        /// <summary>
        /// Attempts to acquire the lock, returning either null for failure or a non-null state "cookie" on success
        /// </summary>
        Task<TLockCookie> TryAcquireAsync(ConnectionOrTransaction connectionOrTransaction, string resourceName, int timeoutMillis, CancellationToken cancellationToken);
        
        void Release(ConnectionOrTransaction connectionOrTransaction, string resourceName, TLockCookie lockCookie);
    }
}
