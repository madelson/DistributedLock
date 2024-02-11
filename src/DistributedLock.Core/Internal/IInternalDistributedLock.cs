namespace Medallion.Threading.Internal;

#if DEBUG
public
#else
internal
#endif
interface IInternalDistributedLock<THandle> : IDistributedLock
    where THandle : class, IDistributedSynchronizationHandle
{
    new THandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default);
    new THandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default);
    new ValueTask<THandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default);
    new ValueTask<THandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);

    // internals
    ValueTask<THandle?> InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken);
}
