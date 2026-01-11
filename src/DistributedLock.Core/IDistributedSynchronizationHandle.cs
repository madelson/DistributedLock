namespace Medallion.Threading;

/// <summary>
/// A handle to a distributed lock or other synchronization primitive. To unlock/release,
/// simply dispose the handle.
/// </summary>
public interface IDistributedSynchronizationHandle
    : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets a <see cref="CancellationToken"/> instance which may be used to 
    /// monitor whether the handle to the lock is lost before the handle is
    /// disposed. 
    /// 
    /// For example, this could happen if the lock is backed by a 
    /// database and the connection to the database is disrupted.
    /// 
    /// Not all lock types support this; those that don't will return <see cref="CancellationToken.None"/>
    /// which can be detected by checking <see cref="CancellationToken.CanBeCanceled"/>.
    /// 
    /// For lock types that do support this, accessing this property may incur additional
    /// costs, such as polling to detect connectivity loss. In general, it is only recommended
    /// when you (a) will be holding a lock for a long time, (b) have experienced/expect flakiness in holding
    /// a lock, and (c) are very sensitive to the lock semantics being violated.
    /// </summary>
    CancellationToken HandleLostToken { get; }
}
