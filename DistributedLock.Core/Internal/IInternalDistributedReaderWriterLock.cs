using System;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Internal
{
#if DEBUG
    public
#else
    internal
#endif
    interface IInternalDistributedReaderWriterLock<THandle> : IDistributedReaderWriterLock
        where THandle : class, IDistributedSynchronizationHandle
    {
        new THandle? TryAcquireReadLock(TimeSpan timeout = default, CancellationToken cancellationToken = default);
        new THandle AcquireReadLock(TimeSpan? timeout = null, CancellationToken cancellationToken = default);
        new ValueTask<THandle?> TryAcquireReadLockAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default);
        new ValueTask<THandle> AcquireReadLockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);
        new THandle? TryAcquireWriteLock(TimeSpan timeout = default, CancellationToken cancellationToken = default);
        new THandle AcquireWriteLock(TimeSpan? timeout = null, CancellationToken cancellationToken = default);
        new ValueTask<THandle?> TryAcquireWriteLockAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default);
        new ValueTask<THandle> AcquireWriteLockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);

        // internals
        ValueTask<THandle?> InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken, bool isWrite);
    }
}
