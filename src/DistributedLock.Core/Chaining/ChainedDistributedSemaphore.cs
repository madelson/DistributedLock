using Medallion.Threading.Internal;

namespace Medallion.Threading.Chaining;

/// <summary>
/// A chained semaphore which acquires multiple underlying semaphores
/// </summary>
public class ChainedDistributedSemaphore : IDistributedSemaphore
{
    private readonly IDistributedSemaphore[] _distributedSemaphores;

    internal ChainedDistributedSemaphore(string name, int maxCount, IDistributedSemaphore[] distributedSemaphores)
    {
        this.Name = name ?? throw new ArgumentNullException(nameof(name));
        this.MaxCount = maxCount;
        this._distributedSemaphores = ChainedHelpers.Validate(distributedSemaphores, nameof(distributedSemaphores));
    }

    public string Name { get; }

    public int MaxCount { get; }

    public IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        SyncViaAsync.Run(static s => s.@this.AcquireAsync(s.timeout, s.cancellationToken), (@this: this, timeout, cancellationToken));

    public ValueTask<IDistributedSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        this.TryAcquireInternalAsync(timeout, cancellationToken).GetHandleOrTimeout();

    public IDistributedSynchronizationHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        SyncViaAsync.Run(static s => s.@this.TryAcquireAsync(s.timeout, s.cancellationToken), (@this: this, timeout, cancellationToken));

    public ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        this.TryAcquireInternalAsync(timeout, cancellationToken).GetHandleOrDefault();

    private ValueTask<CompositeDistributedSynchronizationHandle.AcquireResult> TryAcquireInternalAsync(
        TimeoutValue timeout,
        CancellationToken cancellationToken) =>
        CompositeDistributedSynchronizationHandle.TryAcquireAllAsync(
            this._distributedSemaphores,
            static (s, t, c) => SyncViaAsync.IsSynchronous ? s.TryAcquire(t, c).AsValueTask() : s.TryAcquireAsync(t, c),
            timeout,
            cancellationToken);
}
