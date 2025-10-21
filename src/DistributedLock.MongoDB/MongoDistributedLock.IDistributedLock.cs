using Medallion.Threading.Internal;

namespace Medallion.Threading.MongoDB;

public partial class MongoDistributedLock
{
    IDistributedSynchronizationHandle? IDistributedLock.TryAcquire(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return TryAcquire(timeout, cancellationToken);
    }

    IDistributedSynchronizationHandle IDistributedLock.Acquire(TimeSpan? timeout, CancellationToken cancellationToken)
    {
        return Acquire(timeout, cancellationToken);
    }

    ValueTask<IDistributedSynchronizationHandle?> IDistributedLock.TryAcquireAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return TryAcquireAsync(timeout, cancellationToken).Convert(To<IDistributedSynchronizationHandle?>.ValueTask);
    }

    ValueTask<IDistributedSynchronizationHandle> IDistributedLock.AcquireAsync(TimeSpan? timeout, CancellationToken cancellationToken)
    {
        return AcquireAsync(timeout, cancellationToken).Convert(To<IDistributedSynchronizationHandle>.ValueTask);
    }

    /// <summary>
    /// Attempts to acquire the lock synchronously. Usage:
    /// <code>
    ///     using (var handle = myLock.TryAcquire(...))
    ///     {
    ///         if (handle != null) { /* we have the lock! */ }
    ///     }
    ///     // dispose releases the lock if we took it
    /// </code>
    /// </summary>
    /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to 0</param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
    /// <returns>A <see cref="MongoDistributedLockHandle" /> which can be used to release the lock or null on failure</returns>
    public MongoDistributedLockHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        return DistributedLockHelpers.TryAcquire(this, timeout, cancellationToken);
    }

    /// <summary>
    /// Acquires the lock synchronously, failing with <see cref="TimeoutException" /> if the attempt times out. Usage:
    /// <code>
    ///     using (myLock.Acquire(...))
    ///     {
    ///         /* we have the lock! */
    ///     }
    ///     // dispose releases the lock
    /// </code>
    /// </summary>
    /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to <see cref="Timeout.InfiniteTimeSpan" /></param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
    /// <returns>A <see cref="MongoDistributedLockHandle" /> which can be used to release the lock</returns>
    public MongoDistributedLockHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        return DistributedLockHelpers.Acquire(this, timeout, cancellationToken);
    }

    /// <summary>
    /// Attempts to acquire the lock asynchronously. Usage:
    /// <code>
    ///     await using (var handle = await myLock.TryAcquireAsync(...))
    ///     {
    ///         if (handle != null) { /* we have the lock! */ }
    ///     }
    ///     // dispose releases the lock if we took it
    /// </code>
    /// </summary>
    /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to 0</param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
    /// <returns>A <see cref="MongoDistributedLockHandle" /> which can be used to release the lock or null on failure</returns>
    public ValueTask<MongoDistributedLockHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        return this.As<IInternalDistributedLock<MongoDistributedLockHandle>>().InternalTryAcquireAsync(timeout, cancellationToken);
    }

    /// <summary>
    /// Acquires the lock asynchronously, failing with <see cref="TimeoutException" /> if the attempt times out. Usage:
    /// <code>
    ///     await using (await myLock.AcquireAsync(...))
    ///     {
    ///         /* we have the lock! */
    ///  }
    ///     // dispose releases the lock
    /// </code>
    /// </summary>
    /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to <see cref="Timeout.InfiniteTimeSpan" /></param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
    /// <returns>A <see cref="MongoDistributedLockHandle" /> which can be used to release the lock</returns>
    public ValueTask<MongoDistributedLockHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        return DistributedLockHelpers.AcquireAsync(this, timeout, cancellationToken);
    }
}