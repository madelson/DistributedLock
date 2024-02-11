using Medallion.Threading.Internal;

namespace Medallion.Threading.Azure;

/// <summary>
/// Implements <see cref="IDistributedSynchronizationHandle"/>
/// </summary>
public sealed class AzureBlobLeaseDistributedLockHandle : IDistributedSynchronizationHandle
{
    private AzureBlobLeaseDistributedLock.InternalHandle? _internalHandle;
    private IDisposable? _finalizerRegistration;

    internal AzureBlobLeaseDistributedLockHandle(AzureBlobLeaseDistributedLock.InternalHandle internalHandle)
    {
        this._internalHandle = internalHandle;
        // Because this is a lease, managed finalization mostly won't be strictly necessary here. Where it comes in handy is:
        // (1) Ensuring blob deletion if we own the blob
        // (2) Helping release infinite-duration leases (rare case)
        // (3) In testing, avoiding having to wait 15+ seconds for lease expiration
        this._finalizerRegistration = ManagedFinalizerQueue.Instance.Register(this, internalHandle);
    }

    /// <summary>
    /// Implements <see cref="IDistributedSynchronizationHandle.HandleLostToken"/>
    /// </summary>
    public CancellationToken HandleLostToken => (this._internalHandle ?? throw this.ObjectDisposed()).HandleLostToken;

    /// <summary>
    /// The underlying Azure lease ID
    /// </summary>
    public string LeaseId => (this._internalHandle ?? throw this.ObjectDisposed()).LeaseId;

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
