using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Internal
{
#if DEBUG
    public
#else
    internal
#endif
    interface IInternalDistributedLock<THandle> : IDistributedLock
        where THandle : class, IDistributedLockHandle
    {
        new THandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default);
        new THandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default);
        new ValueTask<THandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default);
        new ValueTask<THandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);

        // internals
        ValueTask<THandle?> InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken);
        /// <summary>
        /// In a sync-over-async scenario, determines whether the code will need to go async anyway
        /// </summary>
        bool WillGoAsync(TimeoutValue timeout, CancellationToken cancellationToken);
    }
}
