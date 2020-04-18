using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Azure
{
    public sealed class AzureBlobLeaseDistributedLockHandle : IDistributedLockHandle
    {
        private AzureBlobLeaseDistributedLock.InternalHandle? _internalHandle;
        private IDisposable? _finalizerRegistration;

        internal AzureBlobLeaseDistributedLockHandle(AzureBlobLeaseDistributedLock.InternalHandle internalHandle)
        {
            this._internalHandle = internalHandle;
            this._finalizerRegistration = ManagedFinalizerQueue.Instance.Register(this, internalHandle);
        }

        public CancellationToken HandleLostToken => (this._internalHandle ?? throw this.ObjectDisposed()).HandleLostToken;

        public string LeaseId => (this._internalHandle ?? throw this.ObjectDisposed()).LeaseId;

        public void Dispose() => SyncOverAsync.Run(@this => @this.DisposeAsync(), this, false);

        public ValueTask DisposeAsync()
        {
            Interlocked.Exchange(ref this._finalizerRegistration, null)?.Dispose();
            return Interlocked.Exchange(ref this._internalHandle, null)?.DisposeAsync() ?? default;
        }
    }
}
