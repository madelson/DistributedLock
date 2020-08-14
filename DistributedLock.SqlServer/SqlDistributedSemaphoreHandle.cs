using Medallion.Threading.Internal;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.SqlServer
{
    /// <summary>
    /// Implements <see cref="IDistributedLockHandle"/>
    /// </summary>
    public sealed class SqlDistributedSemaphoreHandle : IDistributedLockHandle
    {
        private IDistributedLockHandle? _innerHandle;

        internal SqlDistributedSemaphoreHandle(IDistributedLockHandle innerHandle)
        {
            this._innerHandle = innerHandle;
        }

        /// <summary>
        /// Implements <see cref="IDistributedLockHandle.HandleLostToken"/>
        /// </summary>
        public CancellationToken HandleLostToken => this._innerHandle?.HandleLostToken ?? throw this.ObjectDisposed();

        /// <summary>
        /// Releases the semaphore
        /// </summary>
        public void Dispose() => Interlocked.Exchange(ref this._innerHandle, null)?.Dispose();

        /// <summary>
        /// Releases the semaphore asynchronously
        /// </summary>
        public ValueTask DisposeAsync() => Interlocked.Exchange(ref this._innerHandle, null)?.DisposeAsync() ?? default;
    }
}
