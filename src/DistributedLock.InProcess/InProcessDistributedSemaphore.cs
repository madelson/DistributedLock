using Medallion.Threading.Internal;

namespace Medallion.Threading.InProcess;

/// <summary>
/// Implements a non-distributed semaphore based on a shared <see cref="SemaphoreSlim"/>. Instances are created via
/// <see cref="InProcessDistributedSynchronizationProvider"/>, which shares the underlying primitive by scope and name.
/// A handle which is abandoned without being disposed is released only after it has been garbage collected and
/// processed by a background finalizer (roughly 30 seconds); until then its ticket remains taken.
/// </summary>
public sealed partial class InProcessDistributedSemaphore : IInternalDistributedSemaphore<InProcessDistributedSemaphoreHandle>
{
    private readonly SemaphoreSlim _semaphore;

    internal InProcessDistributedSemaphore(string name, int maxCount, SemaphoreSlim semaphore)
    {
        this.Name = name ?? throw new ArgumentNullException(nameof(name));
        this.MaxCount = maxCount;
        this._semaphore = semaphore ?? throw new ArgumentNullException(nameof(semaphore));
    }

    /// <summary>
    /// Implements <see cref="IDistributedSemaphore.Name"/>
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Implements <see cref="IDistributedSemaphore.MaxCount"/>
    /// </summary>
    public int MaxCount { get; }

    async ValueTask<InProcessDistributedSemaphoreHandle?> IInternalDistributedSemaphore<InProcessDistributedSemaphoreHandle>.InternalTryAcquireAsync(
        TimeoutValue timeout,
        CancellationToken cancellationToken) =>
        await this._semaphore.InternalWaitAsync(timeout.TimeSpan, cancellationToken).ConfigureAwait(false)
            ? new InProcessDistributedSemaphoreHandle(this._semaphore)
            : null;
}
