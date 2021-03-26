using System;
using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading.Internal;

namespace Medallion.Threading.ZooKeeper
{
    public partial class ZooKeeperDistributedSemaphore
    {
        // AUTO-GENERATED

        IDistributedSynchronizationHandle? IDistributedSemaphore.TryAcquire(TimeSpan timeout, CancellationToken cancellationToken) =>
            this.As<IInternalDistributedSemaphore<ZooKeeperDistributedSemaphoreHandle>>().TryAcquire(timeout, cancellationToken);
        IDistributedSynchronizationHandle IDistributedSemaphore.Acquire(TimeSpan? timeout, CancellationToken cancellationToken) =>
            this.As<IInternalDistributedSemaphore<ZooKeeperDistributedSemaphoreHandle>>().Acquire(timeout, cancellationToken);
        ValueTask<IDistributedSynchronizationHandle?> IDistributedSemaphore.TryAcquireAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
            this.TryAcquireAsync(timeout, cancellationToken).Convert(To<IDistributedSynchronizationHandle?>.ValueTask);
        ValueTask<IDistributedSynchronizationHandle> IDistributedSemaphore.AcquireAsync(TimeSpan? timeout, CancellationToken cancellationToken) =>
            this.AcquireAsync(timeout, cancellationToken).Convert(To<IDistributedSynchronizationHandle>.ValueTask);

        ZooKeeperDistributedSemaphoreHandle? IInternalDistributedSemaphore<ZooKeeperDistributedSemaphoreHandle>.TryAcquire(TimeSpan timeout, CancellationToken cancellationToken) =>
            DistributedLockHelpers.TryAcquire(this, timeout, cancellationToken);

        ZooKeeperDistributedSemaphoreHandle IInternalDistributedSemaphore<ZooKeeperDistributedSemaphoreHandle>.Acquire(TimeSpan? timeout, CancellationToken cancellationToken) =>
            DistributedLockHelpers.Acquire(this, timeout, cancellationToken);

        /// <summary>
        /// Attempts to acquire a semaphore ticket asynchronously. Usage: 
        /// <code>
        ///     await using (var handle = await mySemaphore.TryAcquireAsync(...))
        ///     {
        ///         if (handle != null) { /* we have the ticket! */ }
        ///     }
        ///     // dispose releases the ticket if we took it
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to 0</param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>A <see cref="ZooKeeperDistributedSemaphoreHandle"/> which can be used to release the ticket or null on failure</returns>
        public ValueTask<ZooKeeperDistributedSemaphoreHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            this.As<IInternalDistributedSemaphore<ZooKeeperDistributedSemaphoreHandle>>().InternalTryAcquireAsync(timeout, cancellationToken);

        /// <summary>
        /// Acquires a semaphore ticket asynchronously, failing with <see cref="TimeoutException"/> if the attempt times out. Usage: 
        /// <code>
        ///     await using (await mySemaphore.AcquireAsync(...))
        ///     {
        ///         /* we have the ticket! */
        ///     }
        ///     // dispose releases the ticket
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to <see cref="Timeout.InfiniteTimeSpan"/></param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>A <see cref="ZooKeeperDistributedSemaphoreHandle"/> which can be used to release the ticket</returns>
        public ValueTask<ZooKeeperDistributedSemaphoreHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            DistributedLockHelpers.AcquireAsync(this, timeout, cancellationToken);
    }
}