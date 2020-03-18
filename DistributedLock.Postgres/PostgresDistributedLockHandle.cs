using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Postgres
{
    // todo this whole file could be auto-generated
    public sealed class PostgresDistributedLockHandle : IDistributedLockHandle
    {
        private IDistributedLockHandle? _innerHandle;

        internal PostgresDistributedLockHandle(IDistributedLockHandle innerHandle)
        {
            this._innerHandle = innerHandle;
        }

        public CancellationToken HandleLostToken => this._innerHandle?.HandleLostToken ?? throw this.ObjectDisposed();

        public void Dispose() => Interlocked.Exchange(ref this._innerHandle, null)?.Dispose();

        public ValueTask DisposeAsync() => Interlocked.Exchange(ref this._innerHandle, null)?.DisposeAsync() ?? default;
    }
}
