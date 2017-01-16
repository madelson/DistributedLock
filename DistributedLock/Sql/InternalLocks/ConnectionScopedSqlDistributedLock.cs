using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    internal sealed class ConnectionScopedSqlDistributedLock : IInternalSqlDistributedLock
    {
        private readonly string lockName;
        private readonly DbConnection connection;

        public ConnectionScopedSqlDistributedLock(string lockName, DbConnection connection)
        {
            this.lockName = lockName;
            this.connection = connection;
        }

        public IDisposable TryAcquire(int timeoutMillis, SqlApplicationLock.Mode mode, IDisposable contextHandle)
        {
            this.CheckConnection();

            return SqlApplicationLock.ExecuteAcquireCommand(this.connection, this.lockName, timeoutMillis, mode)
                ? new LockScope(this)
                : null;
        }

        public async Task<IDisposable> TryAcquireAsync(int timeoutMillis, SqlApplicationLock.Mode mode, CancellationToken cancellationToken, IDisposable contextHandle)
        {
            this.CheckConnection();

            return await SqlApplicationLock.ExecuteAcquireCommandAsync(this.connection, this.lockName, timeoutMillis, mode, cancellationToken).ConfigureAwait(false)
                ? new LockScope(this)
                : null;
        }

        private void CheckConnection()
        {
            if (this.connection.State != ConnectionState.Open)
                throw new InvalidOperationException("The connection is not open");
        }

        private void Release()
        {
            if (this.connection.IsClosedOrBroken())
            {
                // lost the connection, so the lock was already released released
                return;
            }

            SqlApplicationLock.ExecuteReleaseCommand(this.connection, this.lockName);
        }

        private sealed class LockScope : IDisposable
        {
            private ConnectionScopedSqlDistributedLock @lock;

            public LockScope(ConnectionScopedSqlDistributedLock @lock)
            {
                this.@lock = @lock;
            }
            
            public void Dispose() => Interlocked.Exchange(ref this.@lock, null)?.Release();
        }
    }
}
