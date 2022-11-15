using Medallion.Threading.Internal;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Internal.Data;

/// <summary>
/// There are several strategies for implementing SQL-based locks; this interface
/// abstracts between them to keep the implementation of <see cref="IDistributedLock"/> manageable
/// </summary>
#if DEBUG
public
#else
internal
#endif
interface IDbDistributedLock
{
    // the contextHandle argument to this method is used when acquiring a nested lock, such as upgrading
    // from an upgradeable read lock to a write lock. This allows the implementation to use the same connection
    // for the nested lock

    ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync<TLockCookie>(TimeoutValue timeout, IDbSynchronizationStrategy<TLockCookie> strategy, CancellationToken cancellationToken, IDistributedSynchronizationHandle? contextHandle)
        where TLockCookie : class;
}
