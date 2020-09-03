using System;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading
{
    /// <summary>
    /// Provides distributed locking functionality comparable to <see cref="ReaderWriterLock"/>
    /// </summary>
    public interface IDistributedReaderWriterLock
    {
        /// <summary>
        /// A name that uniquely identifies the lock
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Attempts to acquire a READ lock synchronously. Multiple readers are allowed. Not compatible with a WRITE lock. Usage: 
        /// <code>
        ///     using (var handle = myLock.TryAcquireReadLock(...))
        ///     {
        ///         if (handle != null) { /* we have the lock! */ }
        ///     }
        ///     // dispose releases the lock if we took it
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to 0</param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>An <see cref="IDistributedLockHandle"/> which can be used to release the lock or null on failure</returns>
        IDistributedLockHandle? TryAcquireReadLock(TimeSpan timeout = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Acquires a READ lock synchronously, failing with <see cref="TimeoutException"/> if the attempt times out. Multiple readers are allowed. Not compatible with a WRITE lock. Usage: 
        /// <code>
        ///     using (myLock.AcquireReadLock(...))
        ///     {
        ///         /* we have the lock! */
        ///     }
        ///     // dispose releases the lock
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to <see cref="Timeout.InfiniteTimeSpan"/></param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>An <see cref="IDistributedLockHandle"/> which can be used to release the lock</returns>
        IDistributedLockHandle AcquireReadLock(TimeSpan? timeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts to acquire a READ lock asynchronously. Multiple readers are allowed. Not compatible with a WRITE lock. Usage: 
        /// <code>
        ///     await using (var handle = await myLock.TryAcquireReadLockAsync(...))
        ///     {
        ///         if (handle != null) { /* we have the lock! */ }
        ///     }
        ///     // dispose releases the lock if we took it
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to 0</param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>An <see cref="IDistributedLockHandle"/> which can be used to release the lock or null on failure</returns>
        ValueTask<IDistributedLockHandle?> TryAcquireReadLockAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Acquires a READ lock asynchronously, failing with <see cref="TimeoutException"/> if the attempt times out. Multiple readers are allowed. Not compatible with a WRITE lock. Usage: 
        /// <code>
        ///     await using (await myLock.AcquireReadLockAsync(...))
        ///     {
        ///         /* we have the lock! */
        ///     }
        ///     // dispose releases the lock
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to <see cref="Timeout.InfiniteTimeSpan"/></param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>An <see cref="IDistributedLockHandle"/> which can be used to release the lock</returns>
        ValueTask<IDistributedLockHandle> AcquireReadLockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts to acquire a WRITE lock synchronously. Not compatible with another WRITE lock or an UPGRADE lock. Usage: 
        /// <code>
        ///     using (var handle = myLock.TryAcquireWriteLock(...))
        ///     {
        ///         if (handle != null) { /* we have the lock! */ }
        ///     }
        ///     // dispose releases the lock if we took it
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to 0</param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>An <see cref="IDistributedLockHandle"/> which can be used to release the lock or null on failure</returns>
        IDistributedLockHandle? TryAcquireWriteLock(TimeSpan timeout = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Acquires a WRITE lock synchronously, failing with <see cref="TimeoutException"/> if the attempt times out. Not compatible with another WRITE lock or an UPGRADE lock. Usage: 
        /// <code>
        ///     using (myLock.AcquireWriteLock(...))
        ///     {
        ///         /* we have the lock! */
        ///     }
        ///     // dispose releases the lock
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to <see cref="Timeout.InfiniteTimeSpan"/></param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>An <see cref="IDistributedLockHandle"/> which can be used to release the lock</returns>
        IDistributedLockHandle AcquireWriteLock(TimeSpan? timeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts to acquire a WRITE lock asynchronously. Not compatible with another WRITE lock or an UPGRADE lock. Usage: 
        /// <code>
        ///     await using (var handle = await myLock.TryAcquireWriteLockAsync(...))
        ///     {
        ///         if (handle != null) { /* we have the lock! */ }
        ///     }
        ///     // dispose releases the lock if we took it
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to 0</param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>An <see cref="IDistributedLockHandle"/> which can be used to release the lock or null on failure</returns>
        ValueTask<IDistributedLockHandle?> TryAcquireWriteLockAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Acquires a WRITE lock asynchronously, failing with <see cref="TimeoutException"/> if the attempt times out. Not compatible with another WRITE lock or an UPGRADE lock. Usage: 
        /// <code>
        ///     await using (await myLock.AcquireWriteLockAsync(...))
        ///     {
        ///         /* we have the lock! */
        ///     }
        ///     // dispose releases the lock
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to <see cref="Timeout.InfiniteTimeSpan"/></param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>An <see cref="IDistributedLockHandle"/> which can be used to release the lock</returns>
        ValueTask<IDistributedLockHandle> AcquireWriteLockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);
    }
}
