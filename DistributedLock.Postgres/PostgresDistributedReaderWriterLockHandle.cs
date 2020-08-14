using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Postgres
{
    /// <summary>
    /// Implements <see cref="IDistributedLockHandle"/>
    /// </summary>
    public sealed class PostgresDistributedReaderWriterLockHandle : IDistributedLockHandle
    {
        private IDistributedLockHandle? _innerHandle;

        internal PostgresDistributedReaderWriterLockHandle(IDistributedLockHandle innerHandle)
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
