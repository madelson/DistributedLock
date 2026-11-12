using Medallion.Threading.Internal;

namespace Medallion.Threading.InProcess;

/// <summary>
/// Implements <see cref="IDistributedSynchronizationHandle"/>
/// </summary>
public abstract class InProcessDistributedReaderWriterLockHandle : IDistributedSynchronizationHandle
{
    private readonly LockReleaser _lockReleaser;
    private IDisposable? _finalizerRegistration;

    internal InProcessDistributedReaderWriterLockHandle(AsyncReaderWriterLockState state, InProcessReaderWriterLockType lockType)
    {
        this._lockReleaser = new LockReleaser(state, lockType);
        // We need a managed finalizer here because the underlying state is shared by all handles for the name;
        // an abandoned handle would otherwise hold its read/write claim for as long as that state stays alive.
        this._finalizerRegistration = ManagedFinalizerQueue.Instance.Register(this, this._lockReleaser);
    }

    private protected bool IsDisposed => Volatile.Read(ref this._finalizerRegistration) is null;

    private protected LockReleaser Releaser => this._lockReleaser;

    /// <summary>
    /// Implements <see cref="IDistributedSynchronizationHandle.HandleLostToken"/>.
    /// An in-process handle can never be lost, so this is always <see cref="CancellationToken.None"/>
    /// until the handle is disposed.
    /// </summary>
    public CancellationToken HandleLostToken =>
        !this.IsDisposed ? CancellationToken.None : throw this.ObjectDisposed();

    /// <summary>
    /// Releases the lock
    /// </summary>
    public void Dispose()
    {
        Interlocked.Exchange(ref this._finalizerRegistration, null)?.Dispose();
        this._lockReleaser.Dispose();
    }

    /// <summary>
    /// Releases the lock
    /// </summary>
    public ValueTask DisposeAsync()
    {
        this.Dispose();
        return default;
    }

    private protected class LockReleaser(AsyncReaderWriterLockState state, InProcessReaderWriterLockType lockType)
        : IDisposable, IAsyncDisposable
    {
        private const int NotUpgradedState = 0, UpgradedState = 1, DisposedState = 2;

        private readonly AsyncReaderWriterLockState _state = state;
        private int _upgradeState = NotUpgradedState;

        public AsyncReaderWriterLockState State => this._state;

        /// <summary>
        /// Records that the caller has successfully taken the write claim, transferring responsibility for
        /// releasing it to <see cref="Dispose"/>. Returns false if the handle was already disposed, in which
        /// case the caller still owns the write claim and must release it via
        /// <see cref="AsyncReaderWriterLockState.ReleaseWriteCore"/>.
        /// </summary>
        public bool TryMarkUpgraded() =>
            Interlocked.CompareExchange(ref this._upgradeState, UpgradedState, NotUpgradedState) == NotUpgradedState;

        public void Dispose()
        {
            var previousState = Interlocked.Exchange(ref this._upgradeState, DisposedState);
            if (previousState != DisposedState)
            {
                this._state.Release(lockType, upgraded: previousState == UpgradedState);
            }
        }

        public ValueTask DisposeAsync()
        {
            this.Dispose();
            return default;
        }
    }
}

internal sealed class InProcessDistributedReaderWriterLockNonUpgradeableHandle(AsyncReaderWriterLockState state, bool isWrite)
    : InProcessDistributedReaderWriterLockHandle(state, isWrite ? InProcessReaderWriterLockType.Write : InProcessReaderWriterLockType.Read)
{
}

/// <summary>
/// Implements <see cref="IDistributedLockUpgradeableHandle"/>
/// </summary>
public sealed class InProcessDistributedReaderWriterLockUpgradeableHandle
    : InProcessDistributedReaderWriterLockHandle, IInternalDistributedLockUpgradeableHandle
{
    private int _upgradeAttemptState; // 0 = no upgrade in flight or completed, 1 = in flight or upgraded

    internal InProcessDistributedReaderWriterLockUpgradeableHandle(AsyncReaderWriterLockState state)
        : base(state, InProcessReaderWriterLockType.Upgrade)
    {
    }

    /// <summary>
    /// Implements <see cref="IDistributedLockUpgradeableHandle.TryUpgradeToWriteLock(TimeSpan, CancellationToken)"/>
    /// </summary>
    public bool TryUpgradeToWriteLock(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        DistributedLockHelpers.TryUpgradeToWriteLock(this, timeout, cancellationToken);

    /// <summary>
    /// Implements <see cref="IDistributedLockUpgradeableHandle.TryUpgradeToWriteLockAsync(TimeSpan, CancellationToken)"/>
    /// </summary>
    public ValueTask<bool> TryUpgradeToWriteLockAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        this.As<IInternalDistributedLockUpgradeableHandle>().InternalTryUpgradeToWriteLockAsync(timeout, cancellationToken);

    /// <summary>
    /// Implements <see cref="IDistributedLockUpgradeableHandle.UpgradeToWriteLock(TimeSpan?, CancellationToken)"/>
    /// </summary>
    public void UpgradeToWriteLock(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        DistributedLockHelpers.UpgradeToWriteLock(this, timeout, cancellationToken);

    /// <summary>
    /// Implements <see cref="IDistributedLockUpgradeableHandle.UpgradeToWriteLockAsync(TimeSpan?, CancellationToken)"/>
    /// </summary>
    public ValueTask UpgradeToWriteLockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        DistributedLockHelpers.UpgradeToWriteLockAsync(this, timeout, cancellationToken);

    ValueTask<bool> IInternalDistributedLockUpgradeableHandle.InternalTryUpgradeToWriteLockAsync(TimeoutValue timeout, CancellationToken cancellationToken)
    {
        // these validations must throw synchronously
        if (this.IsDisposed) { throw this.ObjectDisposed(); }
        if (Interlocked.CompareExchange(ref this._upgradeAttemptState, 1, 0) != 0)
        {
            throw new InvalidOperationException("the lock has already been upgraded");
        }

        return TryPerformUpgradeAsync();

        async ValueTask<bool> TryPerformUpgradeAsync()
        {
            var releaser = this.Releaser;
            var state = releaser.State;

            bool acquiredWriteClaim;
            try
            {
                // we already hold the upgrade semaphore, so we only need the entry gate + write semaphore
                acquiredWriteClaim = await state.TryAcquireWriteCoreAsync(timeout, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // on cancellation/failure the upgrade never happened, so allow a retry
                Volatile.Write(ref this._upgradeAttemptState, 0);
                throw;
            }

            if (!acquiredWriteClaim)
            {
                Volatile.Write(ref this._upgradeAttemptState, 0); // timed out; allow a retry
                return false;
            }

            if (!releaser.TryMarkUpgraded())
            {
                // The handle was disposed while we were acquiring. Dispose() released the upgrade claim but knows
                // nothing about the write claim we just took, so we must unwind it here.
                state.ReleaseWriteCore();
                throw this.ObjectDisposed();
            }

            return true;
        }
    }
}