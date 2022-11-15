using System;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Internal;

#if DEBUG
public
#else
internal
#endif
interface IInternalDistributedUpgradeableReaderWriterLock<THandle, TUpgradeableHandle> : IDistributedUpgradeableReaderWriterLock, IInternalDistributedReaderWriterLock<THandle>
    where THandle : class, IDistributedSynchronizationHandle
    where TUpgradeableHandle : class, IDistributedLockUpgradeableHandle
{
    new TUpgradeableHandle? TryAcquireUpgradeableReadLock(TimeSpan timeout = default, CancellationToken cancellationToken = default);
    new TUpgradeableHandle AcquireUpgradeableReadLock(TimeSpan? timeout = null, CancellationToken cancellationToken = default);
    new ValueTask<TUpgradeableHandle?> TryAcquireUpgradeableReadLockAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default);
    new ValueTask<TUpgradeableHandle> AcquireUpgradeableReadLockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);

    // internals
    ValueTask<TUpgradeableHandle?> InternalTryAcquireUpgradeableReadLockAsync(TimeoutValue timeout, CancellationToken cancellationToken);
}
