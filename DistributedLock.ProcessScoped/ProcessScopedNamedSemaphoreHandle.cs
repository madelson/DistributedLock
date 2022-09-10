using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading
{
    /// <summary>
    /// Implements <see cref="IDistributedSynchronizationHandle"/>
    /// </summary>
    public sealed class ProcessScopedNamedSemaphoreHandle : IDistributedSynchronizationHandle
    {
        private SemaphoreAndLease? _semaphoreAndLease;
        private IDisposable? _finalizerRegistration;

        internal ProcessScopedNamedSemaphoreHandle(SemaphoreSlim semaphore, IDisposable lease)
        {
            this._semaphoreAndLease = new(semaphore, lease);
            this._finalizerRegistration = ManagedFinalizerQueue.Instance.Register(this, this._semaphoreAndLease);
        }

        CancellationToken IDistributedSynchronizationHandle.HandleLostToken =>
            this._semaphoreAndLease is null ? throw this.ObjectDisposed() : CancellationToken.None;

        /// <summary>
        /// Releases the semaphore
        /// </summary>
        public void Dispose()
        {
            Interlocked.Exchange(ref this._semaphoreAndLease, null)?.Dispose();
            Interlocked.Exchange(ref this._finalizerRegistration, null)?.Dispose();
        }

        /// <summary>
        /// Releases the semaphore
        /// </summary>
        public ValueTask DisposeAsync()
        {
            this.Dispose();
            return default;
        }

        private sealed record SemaphoreAndLease(SemaphoreSlim Semaphore, IDisposable Lease) : IAsyncDisposable, IDisposable
        {
            public void Dispose()
            {
                this.Semaphore.Release();
                this.Lease.Dispose();
            }

            public ValueTask DisposeAsync()
            {
                this.Dispose();
                return default;
            }
        }
    }
}
