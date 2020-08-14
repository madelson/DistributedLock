using Medallion.Threading.Internal;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.SqlServer
{
    /// <summary>
    /// Implements <see cref="IDistributedLockHandle"/>
    /// </summary>
    public sealed class SqlDistributedLockHandle : IDistributedLockHandle
    {
        private IDistributedLockHandle? _innerHandle;

        internal SqlDistributedLockHandle(IDistributedLockHandle innerHandle)
        {
            this._innerHandle = innerHandle;
        }

        /// <summary>
        /// Implements <see cref="IDistributedLockHandle.HandleLostToken"/>
        /// </summary>
        public CancellationToken HandleLostToken => this._innerHandle?.HandleLostToken ?? throw this.ObjectDisposed();

        /// <summary>
        /// Releases the lock
        /// </summary>
        public void Dispose() => Interlocked.Exchange(ref this._innerHandle, null)?.Dispose();

        /// <summary>
        /// Releases the lock asynchronously
        /// </summary>
        public ValueTask DisposeAsync() => Interlocked.Exchange(ref this._innerHandle, null)?.DisposeAsync() ?? default;
    }
}
