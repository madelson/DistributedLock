using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Data
{
    // todo revisit this; right place, right abstraction?
    internal static class SemaphoreSlimHelper
    {
        public static ValueTask<bool> WaitAsync(SemaphoreSlim semaphore, TimeoutValue timeout, CancellationToken cancellationToken)
        {
            if (SyncOverAsync.IsSynchronous)
            {
                semaphore.WaitAsync(timeout.InMilliseconds, cancellationToken);
                return default;
            }

            return semaphore.WaitAsync(timeout.InMilliseconds, cancellationToken).AsValueTask();
        }
    }
}
