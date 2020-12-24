using System;
using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading.Internal;

namespace Medallion.Threading.Redis
{
    public partial class RedisDistributedSemaphore
    {
        // AUTO-GENERATED

        IDistributedLockHandle? IDistributedSemaphore.TryAcquire(TimeSpan timeout, CancellationToken cancellationToken) =>
            this.TryAcquire(timeout, cancellationToken);
        IDistributedLockHandle IDistributedSemaphore.Acquire(TimeSpan? timeout, CancellationToken cancellationToken) =>
            this.Acquire(timeout, cancellationToken);
        ValueTask<IDistributedLockHandle?> IDistributedSemaphore.TryAcquireAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
            this.TryAcquireAsync(timeout, cancellationToken).Convert(To<IDistributedLockHandle?>.ValueTask);
        ValueTask<IDistributedLockHandle> IDistributedSemaphore.AcquireAsync(TimeSpan? timeout, CancellationToken cancellationToken) =>
            this.AcquireAsync(timeout, cancellationToken).Convert(To<IDistributedLockHandle>.ValueTask);

        /// <summary>
        /// Attempts to acquire a semaphore ticket synchronously. Usage: 
        /// <code>
        ///     using (var handle = mySemaphore.TryAcquire(...))
        ///     {
        ///         if (handle != null) { /* we have the ticket! */ }
        ///     }
        ///     // dispose releases the ticket if we took it
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to 0</param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>A <see cref="RedisDistributedSemaphoreHandle"/> which can be used to release the ticket or null on failure</returns>
        public RedisDistributedSemaphoreHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            DistributedLockHelpers.TryAcquire(this, timeout, cancellationToken);

        /// <summary>
        /// Acquires a semaphore ticket synchronously, failing with <see cref="TimeoutException"/> if the attempt times out. Usage: 
        /// <code>
        ///     using (mySemaphore.Acquire(...))
        ///     {
        ///         /* we have the ticket! */
        ///     }
        ///     // dispose releases the ticket
        /// </code>
        /// </summary>
        /// <param name="timeout">How long to wait before giving up on the acquisition attempt. Defaults to <see cref="Timeout.InfiniteTimeSpan"/></param>
        /// <param name="cancellationToken">Specifies a token by which the wait can be canceled</param>
        /// <returns>A <see cref="RedisDistributedSemaphoreHandle"/> which can be used to release the ticket</returns>
        public RedisDistributedSemaphoreHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
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
        /// <returns>A <see cref="RedisDistributedSemaphoreHandle"/> which can be used to release the ticket or null on failure</returns>
        public ValueTask<RedisDistributedSemaphoreHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
            this.As<IInternalDistributedSemaphore<RedisDistributedSemaphoreHandle>>().InternalTryAcquireAsync(timeout, cancellationToken);

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
        /// <returns>A <see cref="RedisDistributedSemaphoreHandle"/> which can be used to release the ticket</returns>
        public ValueTask<RedisDistributedSemaphoreHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
            DistributedLockHelpers.AcquireAsync(this, timeout, cancellationToken);
    }
}