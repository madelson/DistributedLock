namespace Medallion.Threading.Internal;

/// <summary>
/// An async-based, <see cref="SyncViaAsync"/>-friendly mutex based on <see cref="SemaphoreSlim"/>. We don't expose a 
/// <see cref="IDisposable.Dispose"/> method because <see cref="SemaphoreSlim"/> does not require disposal unless its
/// <see cref="SemaphoreSlim.AvailableWaitHandle"/> is accessed
/// </summary>
internal readonly struct AsyncLock
{
    private readonly SemaphoreSlim _semaphore;

    private AsyncLock(SemaphoreSlim semaphore)
    {
        this._semaphore = semaphore;
    }

    public static AsyncLock Create() => new AsyncLock(new SemaphoreSlim(initialCount: 1, maxCount: 1));

    public async ValueTask<IDisposable> AcquireAsync(CancellationToken cancellationToken)
    {
        var handle = await this.TryAcquireAsync(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        Invariant.Require(handle != null);
        return handle!;
    }

    public async ValueTask<IDisposable?> TryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken)
    {
        var acquired = SyncViaAsync.IsSynchronous
            ? this._semaphore.Wait(timeout.InMilliseconds, cancellationToken)
            : await this._semaphore.WaitAsync(timeout.InMilliseconds, cancellationToken).ConfigureAwait(false);
        return acquired ? new Handle(this._semaphore) : null;
    }

    private sealed class Handle : IDisposable
    {
        private SemaphoreSlim? _semaphore;

        public Handle(SemaphoreSlim semaphore)
        {
            this._semaphore = semaphore;
        }

        public void Dispose() => Interlocked.Exchange(ref this._semaphore, null)?.Release();
    }
}
