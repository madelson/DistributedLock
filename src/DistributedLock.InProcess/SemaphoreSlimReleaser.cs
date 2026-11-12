using Medallion.Threading.Internal;

namespace Medallion.Threading.InProcess;

/// <summary>
/// Releases a ticket on a shared <see cref="SemaphoreSlim"/> exactly once. This is separate from the handle so
/// that it can be registered with <see cref="ManagedFinalizerQueue"/> without keeping the handle alive.
/// </summary>
internal sealed class SemaphoreSlimReleaser(SemaphoreSlim semaphore) : IDisposable, IAsyncDisposable
{
    private SemaphoreSlim? _semaphore = semaphore;

    public void Dispose() => Interlocked.Exchange(ref this._semaphore, null)?.Release();

    public ValueTask DisposeAsync()
    {
        this.Dispose();
        return default;
    }
}
