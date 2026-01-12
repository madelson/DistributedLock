using Medallion.Threading.Internal;

namespace Medallion.Threading.MongoDB;

/// <summary>
/// Implements <see cref="IDistributedSynchronizationHandle" /> for <see cref="MongoDistributedLock" />
/// </summary>
public sealed class MongoDistributedLockHandle : IDistributedSynchronizationHandle
{
    private MongoDistributedLock.InnerHandle? _innerHandle;
    private IDisposable? _finalizerRegistration;

    /// <summary>
    /// Implements <see cref="IDistributedSynchronizationHandle.HandleLostToken" />
    /// </summary>
    public CancellationToken HandleLostToken => (this._innerHandle ?? throw this.ObjectDisposed()).HandleLostToken;

    /// <summary>
    /// Gets the fencing token for this lock acquisition. This is a monotonically increasing value
    /// that can be used to detect stale operations when working with external resources.
    /// </summary>
    public long FencingToken { get; }

    internal MongoDistributedLockHandle(MongoDistributedLock.InnerHandle innerHandle, long fencingToken)
    {
        this._innerHandle = innerHandle;
        this.FencingToken = fencingToken;
        // Register for managed finalization so the lock gets released if the handle is GC'd without being disposed
        this._finalizerRegistration = ManagedFinalizerQueue.Instance.Register(this, innerHandle);
    }

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
        return Interlocked.Exchange(ref this._innerHandle, null)?.DisposeAsync() ?? default;
    }
}