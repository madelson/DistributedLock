using System;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading
{
    internal interface IDistributedLock
    {
        IDisposable TryAcquire(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken));
        IDisposable Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken));

        Task<IDisposable> TryAcquireAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken));
        Task<IDisposable> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}
