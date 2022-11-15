using System;
using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading.Internal;

namespace Medallion.Threading.ZooKeeper;

public partial class ZooKeeperDistributedReaderWriterLock
{
    // AUTO-GENERATED

    IDistributedSynchronizationHandle? IDistributedReaderWriterLock.TryAcquireReadLock(TimeSpan timeout, CancellationToken cancellationToken) =>
        this.As<IInternalDistributedReaderWriterLock<ZooKeeperDistributedReaderWriterLockHandle>>().TryAcquireReadLock(timeout, cancellationToken);
    IDistributedSynchronizationHandle IDistributedReaderWriterLock.AcquireReadLock(TimeSpan? timeout, CancellationToken cancellationToken) =>
        this.As<IInternalDistributedReaderWriterLock<ZooKeeperDistributedReaderWriterLockHandle>>().AcquireReadLock(timeout, cancellationToken);
    ValueTask<IDistributedSynchronizationHandle?> IDistributedReaderWriterLock.TryAcquireReadLockAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
        this.TryAcquireReadLockAsync(timeout, cancellationToken).Convert(To<IDistributedSynchronizationHandle?>.ValueTask);
    ValueTask<IDistributedSynchronizationHandle> IDistributedReaderWriterLock.AcquireReadLockAsync(TimeSpan? timeout, CancellationToken cancellationToken) =>
        this.AcquireReadLockAsync(timeout, cancellationToken).Convert(To<IDistributedSynchronizationHandle>.ValueTask);
    IDistributedSynchronizationHandle? IDistributedReaderWriterLock.TryAcquireWriteLock(TimeSpan timeout, CancellationToken cancellationToken) =>
        this.As<IInternalDistributedReaderWriterLock<ZooKeeperDistributedReaderWriterLockHandle>>().TryAcquireWriteLock(timeout, cancellationToken);
    IDistributedSynchronizationHandle IDistributedReaderWriterLock.AcquireWriteLock(TimeSpan? timeout, CancellationToken cancellationToken) =>
        this.As<IInternalDistributedReaderWriterLock<ZooKeeperDistributedReaderWriterLockHandle>>().AcquireWriteLock(timeout, cancellationToken);
    ValueTask<IDistributedSynchronizationHandle?> IDistributedReaderWriterLock.TryAcquireWriteLockAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
        this.TryAcquireWriteLockAsync(timeout, cancellationToken).Convert(To<IDistributedSynchronizationHandle?>.ValueTask);
    ValueTask<IDistributedSynchronizationHandle> IDistributedReaderWriterLock.AcquireWriteLockAsync(TimeSpan? timeout, CancellationToken cancellationToken) =>
        this.AcquireWriteLockAsync(timeout, cancellationToken).Convert(To<IDistributedSynchronizationHandle>.ValueTask);

        ZooKeeperDistributedReaderWriterLockHandle? IInternalDistributedReaderWriterLock<ZooKeeperDistributedReaderWriterLockHandle>.TryAcquireReadLock(TimeSpan timeout, CancellationToken cancellationToken) =>
            DistributedLockHelpers.TryAcquire(this, timeout, cancellationToken, isWrite: false);

        ZooKeeperDistributedReaderWriterLockHandle IInternalDistributedReaderWriterLock<ZooKeeperDistributedReaderWriterLockHandle>.AcquireReadLock(TimeSpan? timeout, CancellationToken cancellationToken) =>
            DistributedLockHelpers.Acquire(this, timeout, cancellationToken, isWrite: false);

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
        /// <returns>A <see cref="ZooKeeperDistributedReaderWriterLockHandle"/> which can be used to release the lock or null on failure</returns>
        public ValueTask<ZooKeeperDistributedReaderWriterLockHandle?> TryAcquireReadLockAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            this.As<IInternalDistributedReaderWriterLock<ZooKeeperDistributedReaderWriterLockHandle>>().InternalTryAcquireAsync(timeout, cancellationToken, isWrite: false);

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
        /// <returns>A <see cref="ZooKeeperDistributedReaderWriterLockHandle"/> which can be used to release the lock</returns>
        public ValueTask<ZooKeeperDistributedReaderWriterLockHandle> AcquireReadLockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            DistributedLockHelpers.AcquireAsync(this, timeout, cancellationToken, isWrite: false);

        ZooKeeperDistributedReaderWriterLockHandle? IInternalDistributedReaderWriterLock<ZooKeeperDistributedReaderWriterLockHandle>.TryAcquireWriteLock(TimeSpan timeout, CancellationToken cancellationToken) =>
            DistributedLockHelpers.TryAcquire(this, timeout, cancellationToken, isWrite: true);

        ZooKeeperDistributedReaderWriterLockHandle IInternalDistributedReaderWriterLock<ZooKeeperDistributedReaderWriterLockHandle>.AcquireWriteLock(TimeSpan? timeout, CancellationToken cancellationToken) =>
            DistributedLockHelpers.Acquire(this, timeout, cancellationToken, isWrite: true);

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
        /// <returns>A <see cref="ZooKeeperDistributedReaderWriterLockHandle"/> which can be used to release the lock or null on failure</returns>
        public ValueTask<ZooKeeperDistributedReaderWriterLockHandle?> TryAcquireWriteLockAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            this.As<IInternalDistributedReaderWriterLock<ZooKeeperDistributedReaderWriterLockHandle>>().InternalTryAcquireAsync(timeout, cancellationToken, isWrite: true);

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
        /// <returns>A <see cref="ZooKeeperDistributedReaderWriterLockHandle"/> which can be used to release the lock</returns>
        public ValueTask<ZooKeeperDistributedReaderWriterLockHandle> AcquireWriteLockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            DistributedLockHelpers.AcquireAsync(this, timeout, cancellationToken, isWrite: true);

}