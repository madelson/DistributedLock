using Medallion.Threading.Internal;

namespace Medallion.Threading.InProcess;

/// <summary>
/// Implements <see cref="IDistributedSynchronizationHandle"/>
/// </summary>
public sealed class InProcessDistributedLockHandle : IDistributedSynchronizationHandle
{
    private readonly SemaphoreSlimReleaser _releaser;
    private IDisposable? _finalizerRegistration;

    internal InProcessDistributedLockHandle(SemaphoreSlim semaphore)
    {
        this._releaser = new SemaphoreSlimReleaser(semaphore);
        // We need a managed finalizer here because the underlying SemaphoreSlim is shared by all handles for the
        // name; an abandoned handle would otherwise leak its ticket for as long as the semaphore stays alive.
        this._finalizerRegistration = ManagedFinalizerQueue.Instance.Register(this, this._releaser);
    }

    /// <summary>
    /// Implements <see cref="IDistributedSynchronizationHandle.HandleLostToken"/>. An in-process handle can never
    /// be lost, so this is always <see cref="CancellationToken.None"/> until the handle is disposed.
    /// </summary>
    public CancellationToken HandleLostToken =>
        Volatile.Read(ref this._finalizerRegistration) != null ? CancellationToken.None : throw this.ObjectDisposed();

    /// <summary>
    /// Releases the lock
    /// </summary>
    public void Dispose()
    {
        Interlocked.Exchange(ref this._finalizerRegistration, null)?.Dispose();
        this._releaser.Dispose();
    }

    /// <summary>
    /// Releases the lock
    /// </summary>
    public ValueTask DisposeAsync()
    {
        this.Dispose();
        return default;
    }
}
