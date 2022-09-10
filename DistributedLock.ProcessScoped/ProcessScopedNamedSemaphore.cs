using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading
{
    /// <summary>
    /// An implementation of <see cref="IDistributedSemaphore"/> which is SCOPED TO JUST THE CURRENT PROCESS and therefore
    /// is NOT TRULY DISTRIBUTED. Therefore, this implementation is intended primarily for testing or scenarios where
    /// name-based locking is useful (e.g. when frequently creating and destroying fine-grained locks).
    /// </summary>
    public sealed partial class ProcessScopedNamedSemaphore : IInternalDistributedSemaphore<ProcessScopedNamedSemaphoreHandle>
    {
        private static readonly NamedObjectPool<SemaphoreBox> NamedObjectPool = new(_ => new());

        /// <summary>
        /// Constructs a semaphore with <paramref name="name"/> and <paramref name="maxCount"/>.
        /// </summary>
        public ProcessScopedNamedSemaphore(string name, int maxCount)
        {
            if (maxCount < 1) { throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, "must be positive"); }
            
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.MaxCount = maxCount;
        }

        /// <summary>
        /// Implements <see cref="IDistributedSemaphore.Name"/>
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Implements <see cref="IDistributedSemaphore.MaxCount"/>
        /// </summary>
        public int MaxCount { get; }

        async ValueTask<ProcessScopedNamedSemaphoreHandle?> IInternalDistributedSemaphore<ProcessScopedNamedSemaphoreHandle>.InternalTryAcquireAsync(
            TimeoutValue timeout, 
            CancellationToken cancellationToken)
        {
            var acquired = false;
            var lease = NamedObjectPool.LeaseObject(this.Name);
            try
            {
                var semaphore = lease.Value.GetSemaphore(this.MaxCount);
                acquired = SyncViaAsync.IsSynchronous
                    ? semaphore.Wait(timeout.InMilliseconds, cancellationToken)
                    : await semaphore.WaitAsync(timeout.InMilliseconds, cancellationToken).ConfigureAwait(false);

                return acquired ? new(semaphore, lease) : null;
            }
            finally
            {
                if (!acquired)
                {
                    lease.Dispose();
                }
            }
        }

        private sealed class SemaphoreBox
        {
            private SemaphoreSlim? _semaphore;

            public SemaphoreSlim GetSemaphore(int maxCount)
            {
                if (this._semaphore is { } semaphore)
                {
                    return semaphore;
                }

                SemaphoreSlim created = new(maxCount, maxCount);
                return Interlocked.CompareExchange(ref this._semaphore, value: created, comparand: null) ?? created;
            }
        }
    }
}
