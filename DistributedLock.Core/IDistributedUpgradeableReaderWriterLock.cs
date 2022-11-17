namespace Medallion.Threading;

/// <summary>
/// Extends <see cref="IDistributedReaderWriterLock"/> with the ability to take an "upgrade" lock. Like a read lock, an upgrade lock 
/// allows for other concurrent read locks, but not for other upgrade or write locks. However, an upgrade lock can also be upgraded to a write
/// lock without releasing the underlying handle.
/// </summary>
public interface IDistributedUpgradeableReaderWriterLock : IDistributedReaderWriterLock
{
    /// <summary>
    /// Attempts to acquire an UPGRADE lock synchronously. Not compatible with another UPGRADE lock or a WRITE lock. Usage: 
    /// <code>
    ///     using (var handle = myLock.TryAcquireUpgradeableReadLock(...))
    ///     {
    ///         if (handle != null) { /* we have the lock! */ }
    ///     }
    ///     // dispose releases the lock if we took it
    /// </code>
    /// </summary>
    /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to 0</param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
    /// <returns>An <see cref="IDistributedLockUpgradeableHandle"/> which can be used to release the lock or null on failure</returns>
    IDistributedLockUpgradeableHandle? TryAcquireUpgradeableReadLock(TimeSpan timeout = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquires an UPGRADE lock synchronously, failing with <see cref="TimeoutException"/> if the attempt times out. Not compatible with another UPGRADE lock or a WRITE lock. Usage: 
    /// <code>
    ///     using (myLock.AcquireUpgradeableReadLock(...))
    ///     {
    ///         /* we have the lock! */
    ///     }
    ///     // dispose releases the lock
    /// </code>
    /// </summary>
    /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to <see cref="Timeout.InfiniteTimeSpan"/></param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
    /// <returns>An <see cref="IDistributedLockUpgradeableHandle"/> which can be used to release the lock</returns>
    IDistributedLockUpgradeableHandle AcquireUpgradeableReadLock(TimeSpan? timeout = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to acquire an UPGRADE lock asynchronously. Not compatible with another UPGRADE lock or a WRITE lock. Usage: 
    /// <code>
    ///     await using (var handle = await myLock.TryAcquireUpgradeableReadLockAsync(...))
    ///     {
    ///         if (handle != null) { /* we have the lock! */ }
    ///     }
    ///     // dispose releases the lock if we took it
    /// </code>
    /// </summary>
    /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to 0</param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
    /// <returns>An <see cref="IDistributedLockUpgradeableHandle"/> which can be used to release the lock or null on failure</returns>
    ValueTask<IDistributedLockUpgradeableHandle?> TryAcquireUpgradeableReadLockAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquires an UPGRADE lock asynchronously, failing with <see cref="TimeoutException"/> if the attempt times out. Not compatible with another UPGRADE lock or a WRITE lock. Usage: 
    /// <code>
    ///     await using (await myLock.AcquireUpgradeableReadLockAsync(...))
    ///     {
    ///         /* we have the lock! */
    ///     }
    ///     // dispose releases the lock
    /// </code>
    /// </summary>
    /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to <see cref="Timeout.InfiniteTimeSpan"/></param>
    /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
    /// <returns>An <see cref="IDistributedLockUpgradeableHandle"/> which can be used to release the lock</returns>
    ValueTask<IDistributedLockUpgradeableHandle> AcquireUpgradeableReadLockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);
}
