using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading
{
    public sealed class ProcessScopedNamedLockHandle : IDistributedSynchronizationHandle
    {
        private HandleAndLease<IDisposable>? _handleAndLease;
        private IDisposable? _finalizerRegistration;

        internal ProcessScopedNamedLockHandle(IDisposable handle, IDisposable lease)
        {
            this._handleAndLease = new(handle, lease);
            this._finalizerRegistration = ManagedFinalizerQueue.Instance.Register(this, this._handleAndLease);
        }

        CancellationToken IDistributedSynchronizationHandle.HandleLostToken => 
            this._handleAndLease is null ? throw this.ObjectDisposed() : CancellationToken.None;

        public void Dispose()
        {
            Interlocked.Exchange(ref this._finalizerRegistration, null)?.Dispose();
            Interlocked.Exchange(ref this._handleAndLease, null)?.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            this.Dispose();
            return default;
        }
    }

    internal sealed record HandleAndLease<TDisposable>(TDisposable LockHandle, IDisposable Lease) : IAsyncDisposable, IDisposable
        where TDisposable : IDisposable
    {
        public void Dispose()
        {
            this.LockHandle.Dispose();
            this.Lease.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            this.Dispose();
            return default;
        }
    }
}
