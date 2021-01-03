using Medallion.Threading.Internal;
using Medallion.Threading.Redis.RedLock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Redis
{
    /// <summary>
    /// Implements <see cref="IDistributedSynchronizationHandle"/> for <see cref="RedisDistributedReaderWriterLock"/>
    /// </summary>
    public sealed class RedisDistributedReaderWriterLockHandle : IDistributedSynchronizationHandle
    {
        private RedLockHandle? _innerHandle;

        internal RedisDistributedReaderWriterLockHandle(RedLockHandle innerHandle)
        {
            this._innerHandle = innerHandle;
        }

        /// <summary>
        /// Implements <see cref="IDistributedSynchronizationHandle.HandleLostToken"/>
        /// </summary>
        public CancellationToken HandleLostToken => Volatile.Read(ref this._innerHandle)?.HandleLostToken ?? throw this.ObjectDisposed();

        /// <summary>
        /// Releases the lock
        /// </summary>
        public void Dispose() => Interlocked.Exchange(ref this._innerHandle, null)?.Dispose();

        /// <summary>
        /// Releases the lock asynchronously
        /// </summary>
        /// <returns></returns>
        public ValueTask DisposeAsync() => Interlocked.Exchange(ref this._innerHandle, null)?.DisposeAsync() ?? default;
    }
}
