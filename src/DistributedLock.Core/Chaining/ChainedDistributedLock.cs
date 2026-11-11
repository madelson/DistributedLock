namespace Medallion.Threading.Chaining;

using Medallion.Threading.Internal;

/// <summary>
/// A chained distributed lock which acquires multiple underlying locks
/// </summary>
public class ChainedDistributedLock : IDistributedLock
{
    private readonly IDistributedLock[] _distributedLocks;

    internal ChainedDistributedLock(string name, IDistributedLock[] distributedLocks)
    {
        this.Name = name ?? throw new ArgumentNullException(nameof(name));
        this._distributedLocks = ChainedHelpers.Validate(distributedLocks, nameof(distributedLocks));
    }

    public string Name { get; }

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
            this._distributedLocks,
            static (l, t, c) => SyncViaAsync.IsSynchronous ? l.TryAcquire(t, c).AsValueTask() : l.TryAcquireAsync(t, c),
            timeout,
            cancellationToken);
}
