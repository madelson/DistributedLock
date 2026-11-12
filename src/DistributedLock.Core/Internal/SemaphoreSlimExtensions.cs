namespace Medallion.Threading.Internal;

#if DEBUG
public
#else
internal
#endif
static class SemaphoreSlimExtensions
{
    /// <summary>
    /// A <see cref="SyncViaAsync"/>-compatible implementation of
    /// <see cref="SemaphoreSlim.WaitAsync(TimeSpan, CancellationToken)"/>.
    /// </summary>
    public static ValueTask<bool> InternalWaitAsync(
        this SemaphoreSlim semaphore,
        TimeSpan timeout,
        CancellationToken cancellationToken) =>
        SyncViaAsync.IsSynchronous
            ? semaphore.Wait(timeout, cancellationToken).AsValueTask()
            : semaphore.WaitAsync(timeout, cancellationToken).AsValueTask();
}
