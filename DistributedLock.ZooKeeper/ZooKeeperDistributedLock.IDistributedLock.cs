using System;
using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading.Internal;

namespace Medallion.Threading.ZooKeeper
{
    public partial class ZooKeeperDistributedLock
    {
        // AUTO-GENERATED

        IDistributedSynchronizationHandle? IDistributedLock.TryAcquire(TimeSpan timeout, CancellationToken cancellationToken) =>
            this.As<IInternalDistributedLock<ZooKeeperDistributedLockHandle>>().TryAcquire(timeout, cancellationToken);
        IDistributedSynchronizationHandle IDistributedLock.Acquire(TimeSpan? timeout, CancellationToken cancellationToken) =>
            this.As<IInternalDistributedLock<ZooKeeperDistributedLockHandle>>().Acquire(timeout, cancellationToken);
        ValueTask<IDistributedSynchronizationHandle?> IDistributedLock.TryAcquireAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
            this.As<IInternalDistributedLock<ZooKeeperDistributedLockHandle>>().TryAcquireAsync(timeout, cancellationToken).Convert(To<IDistributedSynchronizationHandle?>.ValueTask);
        ValueTask<IDistributedSynchronizationHandle> IDistributedLock.AcquireAsync(TimeSpan? timeout, CancellationToken cancellationToken) =>
            this.As<IInternalDistributedLock<ZooKeeperDistributedLockHandle>>().AcquireAsync(timeout, cancellationToken).Convert(To<IDistributedSynchronizationHandle>.ValueTask);

        ZooKeeperDistributedLockHandle? IInternalDistributedLock<ZooKeeperDistributedLockHandle>.TryAcquire(TimeSpan timeout, CancellationToken cancellationToken) =>
            DistributedLockHelpers.TryAcquire(this, timeout, cancellationToken);

        ZooKeeperDistributedLockHandle IInternalDistributedLock<ZooKeeperDistributedLockHandle>.Acquire(TimeSpan? timeout, CancellationToken cancellationToken) =>
            DistributedLockHelpers.Acquire(this, timeout, cancellationToken);

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
        /// <returns>A <see cref="ZooKeeperDistributedLockHandle"/> which can be used to release the lock or null on failure</returns>
        public ValueTask<ZooKeeperDistributedLockHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            this.As<IInternalDistributedLock<ZooKeeperDistributedLockHandle>>().InternalTryAcquireAsync(timeout, cancellationToken);

        /// <summary>
        /// Acquires the lock asynchronously, failing with <see cref="TimeoutException"/> if the attempt times out. Usage: 
        /// <code>
        ///     await using (await myLock.AcquireAsync(...))
        ///     {
        ///         /* we have the lock! */
        ///     }
        ///     // dispose releases the lock
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to <see cref="Timeout.InfiniteTimeSpan"/></param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>A <see cref="ZooKeeperDistributedLockHandle"/> which can be used to release the lock</returns>
        public ValueTask<ZooKeeperDistributedLockHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            DistributedLockHelpers.AcquireAsync(this, timeout, cancellationToken);
    }
}