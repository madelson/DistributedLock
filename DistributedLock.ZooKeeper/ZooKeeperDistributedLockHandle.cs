using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.ZooKeeper
{
    /// <summary>
    /// Implements <see cref="IDistributedSynchronizationHandle"/>
    /// </summary>
    public sealed class ZooKeeperDistributedLockHandle : IDistributedSynchronizationHandle
    {
        private ZooKeeperNodeHandle? _innerHandle;
        private IDisposable? _finalizerRegistration; 

        internal ZooKeeperDistributedLockHandle(ZooKeeperNodeHandle innerHandle)
        {
            this._innerHandle = innerHandle;
            // If the process exits, the fact that we use ephemeral nodes gives us guaranteed
            // abandonment protection. Until that point, though, we're vulnerable to abandonment because
            // we pool zookeeper sessions. While those sessions have a max age, they won't be able to exit
            // so long as a handle to them remains open
            this._finalizerRegistration = ManagedFinalizerQueue.Instance.Register(this, innerHandle);
        }

        /// <summary>
        /// Implements <see cref="IDistributedSynchronizationHandle.HandleLostToken"/>
        /// </summary>
        public CancellationToken HandleLostToken => (Volatile.Read(ref this._innerHandle) ?? throw this.ObjectDisposed()).HandleLostToken;

        // explicit because this is sync-over-async
        void IDisposable.Dispose() => this.DisposeSyncViaAsync();

        /// <summary>
        /// Releases the lock
        /// </summary>
        public ValueTask DisposeAsync()
        {
            Interlocked.Exchange(ref this._finalizerRegistration, null)?.Dispose();
            return Interlocked.Exchange(ref this._innerHandle, null)?.DisposeAsync() ?? default;
        }
    }
}
