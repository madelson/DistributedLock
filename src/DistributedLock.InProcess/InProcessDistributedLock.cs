using Medallion.Threading.Internal;

namespace Medallion.Threading.InProcess;

/// <summary>
/// Implements a non-distributed mutex lock based on a shared <see cref="SemaphoreSlim"/>. Instances are created via
/// <see cref="InProcessDistributedSynchronizationProvider"/>, which shares the underlying primitive by scope and name.
/// A handle which is abandoned without being disposed is released only after it has been garbage collected and
/// processed by a background finalizer (roughly 30 seconds); until then the lock remains held.
/// </summary>
public sealed partial class InProcessDistributedLock : IInternalDistributedLock<InProcessDistributedLockHandle>
{
    private readonly SemaphoreSlim _semaphore;

    internal InProcessDistributedLock(string name, SemaphoreSlim semaphore)
    {
        this.Name = name ?? throw new ArgumentNullException(nameof(name));
        this._semaphore = semaphore ?? throw new ArgumentNullException(nameof(semaphore));
    }

    /// <summary>
    /// Implements <see cref="IDistributedLock.Name"/>
    /// </summary>
    public string Name { get; }

    async ValueTask<InProcessDistributedLockHandle?> IInternalDistributedLock<InProcessDistributedLockHandle>.InternalTryAcquireAsync(
        TimeoutValue timeout,
        CancellationToken cancellationToken) =>
        await this._semaphore.InternalWaitAsync(timeout.TimeSpan, cancellationToken).ConfigureAwait(false)
            ? new InProcessDistributedLockHandle(this._semaphore)
            : null;
}
