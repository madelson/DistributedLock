using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Oracle
{
    /// <summary>
    /// Implements <see cref="IDistributedSynchronizationHandle"/>
    /// </summary>
    public sealed class OracleDistributedLockHandle : IDistributedSynchronizationHandle
    {
        private IDistributedSynchronizationHandle? _innerHandle;

        internal OracleDistributedLockHandle(IDistributedSynchronizationHandle innerHandle)
        {
            this._innerHandle = innerHandle;
        }

        /// <summary>
        /// TODO reference issue
        /// </summary>
        public CancellationToken HandleLostToken => CancellationToken.None;

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
