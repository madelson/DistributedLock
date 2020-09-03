using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.WaitHandles
{
    /// <summary>
    /// Implements <see cref="IDistributedLockHandle"/>
    /// </summary>
    public sealed class WaitHandleDistributedSemaphoreHandle : IDistributedLockHandle
    {
        private readonly SemaphoreReleaser _semaphoreReleaser;
        private IDisposable? _finalizerRegistration;

        internal WaitHandleDistributedSemaphoreHandle(Semaphore semaphore)
        {
            this._semaphoreReleaser = new SemaphoreReleaser(semaphore);
            // We need a managed finalizer here because an abandoned Semaphore instance won't release its tickets unless
            // all instances of that Semaphore are also abandoned (any one live instance tracks the current ticket count).
            this._finalizerRegistration = ManagedFinalizerQueue.Instance.Register(this, this._semaphoreReleaser);
        }

        public CancellationToken HandleLostToken =>
            Volatile.Read(ref this._finalizerRegistration) != null ? CancellationToken.None : throw this.ObjectDisposed();

        public void Dispose()
        {
            Interlocked.Exchange(ref this._finalizerRegistration, null)?.Dispose();
            this._semaphoreReleaser.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            this.Dispose();
            return default;
        }

        private class SemaphoreReleaser : IDisposable, IAsyncDisposable
        {
            private Semaphore? _semaphore;

            public SemaphoreReleaser(Semaphore semaphore)
            {
                this._semaphore = semaphore;
            }

            public void Dispose()
            {
                var semaphore = Interlocked.Exchange(ref this._semaphore, null);
                if (semaphore != null)
                {
                    try { semaphore.Release(); }
                    finally { semaphore.Dispose(); }
                }
            }

            public ValueTask DisposeAsync()
            {
                this.Dispose();
                return default;
            }
        }
    }
}
