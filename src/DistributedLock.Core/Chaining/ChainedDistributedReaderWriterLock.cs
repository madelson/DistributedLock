namespace Medallion.Threading.Chaining;

using Medallion.Threading.Internal;

/// <summary>
/// A chained reader-writer lock which acquires multiple underlying reader-writer locks
/// </summary>
public class ChainedDistributedReaderWriterLock : IDistributedReaderWriterLock
{
    private readonly IDistributedReaderWriterLock[] _distributedReaderWriterLocks;

    internal ChainedDistributedReaderWriterLock(
        string name,
        IDistributedReaderWriterLock[] distributedReaderWriterLocks)
    {
        this.Name = name ?? throw new ArgumentNullException(nameof(name));
        this._distributedReaderWriterLocks = ChainedHelpers.Validate(distributedReaderWriterLocks, nameof(distributedReaderWriterLocks));
    }

    public string Name { get; }

    public IDistributedSynchronizationHandle AcquireReadLock(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        SyncViaAsync.Run(static s => s.@this.AcquireReadLockAsync(s.timeout, s.cancellationToken), (@this: this, timeout, cancellationToken));

    public ValueTask<IDistributedSynchronizationHandle> AcquireReadLockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        this.TryAcquireReadLockInternalAsync(timeout, cancellationToken).GetHandleOrTimeout();

    public IDistributedSynchronizationHandle? TryAcquireReadLock(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        SyncViaAsync.Run(static s => s.@this.TryAcquireReadLockAsync(s.timeout, s.cancellationToken), (@this: this, timeout, cancellationToken));

    public ValueTask<IDistributedSynchronizationHandle?> TryAcquireReadLockAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        this.TryAcquireReadLockInternalAsync(timeout, cancellationToken).GetHandleOrDefault();

    public IDistributedSynchronizationHandle AcquireWriteLock(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        SyncViaAsync.Run(static s => s.@this.AcquireWriteLockAsync(s.timeout, s.cancellationToken), (@this: this, timeout, cancellationToken));

    public ValueTask<IDistributedSynchronizationHandle> AcquireWriteLockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        this.TryAcquireWriteLockInternalAsync(timeout, cancellationToken).GetHandleOrTimeout();

    public IDistributedSynchronizationHandle? TryAcquireWriteLock(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        SyncViaAsync.Run(static s => s.@this.TryAcquireWriteLockAsync(s.timeout, s.cancellationToken), (@this: this, timeout, cancellationToken));

    public ValueTask<IDistributedSynchronizationHandle?> TryAcquireWriteLockAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        this.TryAcquireWriteLockInternalAsync(timeout, cancellationToken).GetHandleOrDefault();

    private ValueTask<CompositeDistributedSynchronizationHandle.AcquireResult> TryAcquireReadLockInternalAsync(
        TimeoutValue timeout,
        CancellationToken cancellationToken) =>
        CompositeDistributedSynchronizationHandle.TryAcquireAllAsync(
            this._distributedReaderWriterLocks,
            static (l, t, c) => SyncViaAsync.IsSynchronous ? l.TryAcquireReadLock(t, c).AsValueTask() : l.TryAcquireReadLockAsync(t, c),
            timeout,
            cancellationToken);

    private ValueTask<CompositeDistributedSynchronizationHandle.AcquireResult> TryAcquireWriteLockInternalAsync(
        TimeoutValue timeout,
        CancellationToken cancellationToken) =>
        CompositeDistributedSynchronizationHandle.TryAcquireAllAsync(
            this._distributedReaderWriterLocks,
            static (l, t, c) => SyncViaAsync.IsSynchronous ? l.TryAcquireWriteLock(t, c).AsValueTask() : l.TryAcquireWriteLockAsync(t, c),
            timeout,
            cancellationToken);
}
