using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading;

/// <summary>
/// A synchronization primitive which restricts access to a resource or critical section of code to a fixed number of concurrent threads/processes.
/// Compare to <see cref="Semaphore"/>.
/// </summary>
public interface IDistributedSemaphore
{
    /// <summary>
    /// A name that uniquely identifies the semaphore
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The maximum number of "tickets" available for the semaphore (ie the number of processes which can acquire
    /// the semaphore concurrently).
    /// </summary>
    int MaxCount { get; }

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
    /// <returns>An <see cref="IDistributedSynchronizationHandle"/> which can be used to release the ticket or null on failure</returns>
    IDistributedSynchronizationHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default);

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
    /// <returns>An <see cref="IDistributedSynchronizationHandle"/> which can be used to release the ticket</returns>
    IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default);

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
    /// <returns>An <see cref="IDistributedSynchronizationHandle"/> which can be used to release the ticket or null on failure</returns>
    ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default);

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
    /// <returns>An <see cref="IDistributedSynchronizationHandle"/> which can be used to release the ticket</returns>
    ValueTask<IDistributedSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);
}
