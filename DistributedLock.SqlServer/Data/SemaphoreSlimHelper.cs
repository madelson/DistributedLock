using Medallion.Threading.Internal;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Data
{
    // todo revisit this; right place, right abstraction?
    internal static class SemaphoreSlimHelper
    {
        public static ValueTask<bool> WaitAsync(SemaphoreSlim semaphore, TimeoutValue timeout, CancellationToken cancellationToken) =>
            SyncOverAsync.IsSynchronous
                ? semaphore.Wait(timeout.InMilliseconds, cancellationToken).AsValueTask()
                : semaphore.WaitAsync(timeout.InMilliseconds, cancellationToken).AsValueTask();
    }
}
