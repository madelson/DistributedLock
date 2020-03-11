using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Internal.Data
{
    /// <summary>
    /// Represents a "locking algorithm" implemented in SQL
    /// </summary>
#if DEBUG
    public
#else
    internal
#endif
    interface IDbSynchronizationStrategy<TLockCookie>
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
        ValueTask<TLockCookie?> TryAcquireAsync(DatabaseConnection connection, string resourceName, TimeoutValue timeout, CancellationToken cancellationToken);

        ValueTask ReleaseAsync(DatabaseConnection connectionconnection, string resourceName, TLockCookie lockCookie);
    }
}
