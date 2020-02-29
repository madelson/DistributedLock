using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading
{
#if DEBUG
    public
#else
    internal 
#endif
        interface IDistributedLockOld
    {
        IDisposable? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default);
        IDisposable Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default);

        Task<IDisposable?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default);
        Task<IDisposable> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);
    }
}
