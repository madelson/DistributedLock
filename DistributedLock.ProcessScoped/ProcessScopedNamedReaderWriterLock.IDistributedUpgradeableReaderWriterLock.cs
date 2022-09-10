using System;
using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading.Internal;

namespace Medallion.Threading
{
    public partial class ProcessScopedNamedReaderWriterLock
    {
        // AUTO-GENERATED

        IDistributedSynchronizationHandle? IDistributedReaderWriterLock.TryAcquireReadLock(TimeSpan timeout, CancellationToken cancellationToken) =>
            this.TryAcquireReadLock(timeout, cancellationToken);
        IDistributedSynchronizationHandle IDistributedReaderWriterLock.AcquireReadLock(TimeSpan? timeout, CancellationToken cancellationToken) =>
            this.AcquireReadLock(timeout, cancellationToken);
        ValueTask<IDistributedSynchronizationHandle?> IDistributedReaderWriterLock.TryAcquireReadLockAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
            this.TryAcquireReadLockAsync(timeout, cancellationToken).Convert(To<IDistributedSynchronizationHandle?>.ValueTask);
        ValueTask<IDistributedSynchronizationHandle> IDistributedReaderWriterLock.AcquireReadLockAsync(TimeSpan? timeout, CancellationToken cancellationToken) =>
            this.AcquireReadLockAsync(timeout, cancellationToken).Convert(To<IDistributedSynchronizationHandle>.ValueTask);
        IDistributedLockUpgradeableHandle? IDistributedUpgradeableReaderWriterLock.TryAcquireUpgradeableReadLock(TimeSpan timeout, CancellationToken cancellationToken) =>
            this.TryAcquireUpgradeableReadLock(timeout, cancellationToken);
        IDistributedLockUpgradeableHandle IDistributedUpgradeableReaderWriterLock.AcquireUpgradeableReadLock(TimeSpan? timeout, CancellationToken cancellationToken) =>
            this.AcquireUpgradeableReadLock(timeout, cancellationToken);
        ValueTask<IDistributedLockUpgradeableHandle?> IDistributedUpgradeableReaderWriterLock.TryAcquireUpgradeableReadLockAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
            this.TryAcquireUpgradeableReadLockAsync(timeout, cancellationToken).Convert(To<IDistributedLockUpgradeableHandle?>.ValueTask);
        ValueTask<IDistributedLockUpgradeableHandle> IDistributedUpgradeableReaderWriterLock.AcquireUpgradeableReadLockAsync(TimeSpan? timeout, CancellationToken cancellationToken) =>
            this.AcquireUpgradeableReadLockAsync(timeout, cancellationToken).Convert(To<IDistributedLockUpgradeableHandle>.ValueTask);
        IDistributedSynchronizationHandle? IDistributedReaderWriterLock.TryAcquireWriteLock(TimeSpan timeout, CancellationToken cancellationToken) =>
            this.TryAcquireWriteLock(timeout, cancellationToken);
        IDistributedSynchronizationHandle IDistributedReaderWriterLock.AcquireWriteLock(TimeSpan? timeout, CancellationToken cancellationToken) =>
            this.AcquireWriteLock(timeout, cancellationToken);
        ValueTask<IDistributedSynchronizationHandle?> IDistributedReaderWriterLock.TryAcquireWriteLockAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
            this.TryAcquireWriteLockAsync(timeout, cancellationToken).Convert(To<IDistributedSynchronizationHandle?>.ValueTask);
        ValueTask<IDistributedSynchronizationHandle> IDistributedReaderWriterLock.AcquireWriteLockAsync(TimeSpan? timeout, CancellationToken cancellationToken) =>
            this.AcquireWriteLockAsync(timeout, cancellationToken).Convert(To<IDistributedSynchronizationHandle>.ValueTask);

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
        /// <returns>A <see cref="ProcessScopedNamedReaderWriterLockHandle"/> which can be used to release the lock or null on failure</returns>
        public ProcessScopedNamedReaderWriterLockHandle? TryAcquireReadLock(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            DistributedLockHelpers.TryAcquire(this, timeout, cancellationToken, isWrite: false);

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
        /// <returns>A <see cref="ProcessScopedNamedReaderWriterLockHandle"/> which can be used to release the lock</returns>
        public ProcessScopedNamedReaderWriterLockHandle AcquireReadLock(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
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
        /// <returns>A <see cref="ProcessScopedNamedReaderWriterLockHandle"/> which can be used to release the lock or null on failure</returns>
        public ValueTask<ProcessScopedNamedReaderWriterLockHandle?> TryAcquireReadLockAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            this.As<IInternalDistributedReaderWriterLock<ProcessScopedNamedReaderWriterLockHandle>>().InternalTryAcquireAsync(timeout, cancellationToken, isWrite: false);

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
        /// <returns>A <see cref="ProcessScopedNamedReaderWriterLockHandle"/> which can be used to release the lock</returns>
        public ValueTask<ProcessScopedNamedReaderWriterLockHandle> AcquireReadLockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            DistributedLockHelpers.AcquireAsync(this, timeout, cancellationToken, isWrite: false);

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
        /// <returns>A <see cref="ProcessScopedNamedReaderWriterLockUpgradeableHandle"/> which can be used to release the lock or null on failure</returns>
        public ProcessScopedNamedReaderWriterLockUpgradeableHandle? TryAcquireUpgradeableReadLock(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            DistributedLockHelpers.TryAcquireUpgradeableReadLock(this, timeout, cancellationToken);

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
        /// <returns>A <see cref="ProcessScopedNamedReaderWriterLockUpgradeableHandle"/> which can be used to release the lock</returns>
        public ProcessScopedNamedReaderWriterLockUpgradeableHandle AcquireUpgradeableReadLock(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            DistributedLockHelpers.AcquireUpgradeableReadLock(this, timeout, cancellationToken);

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
        /// <returns>A <see cref="ProcessScopedNamedReaderWriterLockUpgradeableHandle"/> which can be used to release the lock or null on failure</returns>
        public ValueTask<ProcessScopedNamedReaderWriterLockUpgradeableHandle?> TryAcquireUpgradeableReadLockAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            this.As<IInternalDistributedUpgradeableReaderWriterLock<ProcessScopedNamedReaderWriterLockHandle, ProcessScopedNamedReaderWriterLockUpgradeableHandle>>().InternalTryAcquireUpgradeableReadLockAsync(timeout, cancellationToken);

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
        /// <returns>A <see cref="ProcessScopedNamedReaderWriterLockUpgradeableHandle"/> which can be used to release the lock</returns>
        public ValueTask<ProcessScopedNamedReaderWriterLockUpgradeableHandle> AcquireUpgradeableReadLockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            DistributedLockHelpers.AcquireUpgradeableReadLockAsync(this, timeout, cancellationToken);

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
        /// <returns>A <see cref="ProcessScopedNamedReaderWriterLockHandle"/> which can be used to release the lock or null on failure</returns>
        public ProcessScopedNamedReaderWriterLockHandle? TryAcquireWriteLock(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            DistributedLockHelpers.TryAcquire(this, timeout, cancellationToken, isWrite: true);

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
        /// <returns>A <see cref="ProcessScopedNamedReaderWriterLockHandle"/> which can be used to release the lock</returns>
        public ProcessScopedNamedReaderWriterLockHandle AcquireWriteLock(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
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
        /// <returns>A <see cref="ProcessScopedNamedReaderWriterLockHandle"/> which can be used to release the lock or null on failure</returns>
        public ValueTask<ProcessScopedNamedReaderWriterLockHandle?> TryAcquireWriteLockAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            this.As<IInternalDistributedReaderWriterLock<ProcessScopedNamedReaderWriterLockHandle>>().InternalTryAcquireAsync(timeout, cancellationToken, isWrite: true);

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
        /// <returns>A <see cref="ProcessScopedNamedReaderWriterLockHandle"/> which can be used to release the lock</returns>
        public ValueTask<ProcessScopedNamedReaderWriterLockHandle> AcquireWriteLockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            DistributedLockHelpers.AcquireAsync(this, timeout, cancellationToken, isWrite: true);

    }
}