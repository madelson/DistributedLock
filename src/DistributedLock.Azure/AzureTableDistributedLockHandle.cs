using Medallion.Threading.Internal;

namespace Medallion.Threading.Azure;

/// <summary>
/// Implements <see cref="IDistributedSynchronizationHandle"/>
/// </summary>
public sealed class AzureTableDistributedLockHandle : IDistributedSynchronizationHandle
{
    private AzureTableDistributedLock.InternalHandle? _internalHandle;
    private IDisposable? _finalizerRegistration;

    internal AzureTableDistributedLockHandle(AzureTableDistributedLock.InternalHandle internalHandle)
    {
        this._internalHandle = internalHandle;
        this._finalizerRegistration = ManagedFinalizerQueue.Instance.Register(this, internalHandle);
    }

    /// <summary>
    /// Implements <see cref="IDistributedSynchronizationHandle.HandleLostToken"/>
    /// </summary>
    public CancellationToken HandleLostToken => (this._internalHandle ?? throw this.ObjectDisposed()).HandleLostToken;

    /// <summary>
    /// The unique id proving ownership of the underlying table row
    /// </summary>
    public string LockId => (this._internalHandle ?? throw this.ObjectDisposed()).LockId;

    /// <summary>
    /// Releases the lock
    /// </summary>
    public void Dispose() => this.DisposeSyncViaAsync();

    /// <summary>
    /// Releases the lock asynchronously
    /// </summary>
    public ValueTask DisposeAsync()
    {
        Interlocked.Exchange(ref this._finalizerRegistration, null)?.Dispose();
        return Interlocked.Exchange(ref this._internalHandle, null)?.DisposeAsync() ?? default;
    }
}