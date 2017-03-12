using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    internal sealed class AzureSqlDistributedLock : IInternalSqlDistributedLock
    {
        private readonly string lockName, connectionString;

        public AzureSqlDistributedLock(string lockName, string connectionString)
        {
            this.lockName = lockName;
            this.connectionString = connectionString;
        }

        public IDisposable TryAcquire(int timeoutMillis, SqlApplicationLock.Mode mode, IDisposable contextHandle)
        {
            if (contextHandle != null)
            {
                // if we are taking a nested lock, we don't want to start another keepalive on the same connection.
                // However, we do need to stop our current keepalive while we take the nested lock to avoid threading issues
                var lockScope = (LockScope)contextHandle;
                lockScope.Keepalive.Stop();
                try
                {
                    var internalHandle = lockScope.InternalLock.TryAcquire(timeoutMillis, mode, contextHandle: lockScope.InternalHandle);
                    return internalHandle != null
                        ? new LockScope(internalHandle, lockScope.InternalLock, lockScope.Keepalive, ownsKeepalive: false)
                        : null;
                }
                finally
                {
                    // always restart, even if the acquisition fails
                    lockScope.Keepalive.Start();
                }
            }

            var connection = new SqlConnection(this.connectionString);
            LockScope result = null;
            try
            {
                connection.Open();
                var internalLock = new ConnectionScopedSqlDistributedLock(this.lockName, connection);
                var internalHandle = internalLock.TryAcquire(timeoutMillis, mode, contextHandle: null);
                if (internalHandle != null)
                {
                    var keepalive = new KeepaliveHelper(connection);
                    keepalive.Start();
                    result = new LockScope(internalHandle, internalLock, keepalive, ownsKeepalive: true);
                }
            }
            finally
            {
                if (result == null) { connection.Dispose(); }
            }

            return result;
        }

        public async Task<IDisposable> TryAcquireAsync(int timeoutMillis, SqlApplicationLock.Mode mode, CancellationToken cancellationToken, IDisposable contextHandle)
        {
            if (contextHandle != null)
            {
                cancellationToken.ThrowIfCancellationRequested(); // if already canceled, exit immediately

                // if we are taking a nested lock, we don't want to start another keepalive on the same connection.
                // However, we do need to stop our current keepalive while we take the nested lock to avoid threading issues
                var lockScope = (LockScope)contextHandle;
                await lockScope.Keepalive.StopAsync().ConfigureAwait(false);
                try
                {
                    var internalHandle = await lockScope.InternalLock.TryAcquireAsync(timeoutMillis, mode, cancellationToken, contextHandle: lockScope.InternalHandle).ConfigureAwait(false);
                    return internalHandle != null
                        ? new LockScope(internalHandle, lockScope.InternalLock, lockScope.Keepalive, ownsKeepalive: false)
                        : null;
                }
                finally
                {
                    // always restart, even if the acquisition fails
                    lockScope.Keepalive.Start();
                }
            }

            var connection = new SqlConnection(this.connectionString);
            LockScope result = null;
            try
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                var internalLock = new ConnectionScopedSqlDistributedLock(this.lockName, connection);
                var internalHandle = await internalLock.TryAcquireAsync(timeoutMillis, mode, cancellationToken, contextHandle: null).ConfigureAwait(false);
                if (internalHandle != null)
                {
                    var keepalive = new KeepaliveHelper(connection);
                    keepalive.Start();
                    result = new LockScope(internalHandle, internalLock, keepalive, ownsKeepalive: true);
                }
            }
            finally
            {
                if (result == null) { connection.Dispose(); }
            }

            return result;
        }

        private sealed class LockScope : IDisposable
        {
            private readonly bool ownsKeepalive;
            private KeepaliveHelper keepalive;

            public LockScope(
                IDisposable internalHandle, 
                ConnectionScopedSqlDistributedLock internalLock,
                KeepaliveHelper keepalive,
                bool ownsKeepalive)
            {
                this.InternalHandle = internalHandle;
                this.InternalLock = internalLock;
                this.keepalive = keepalive;
                this.ownsKeepalive = ownsKeepalive;
            }
            
            public IDisposable InternalHandle { get; private set; }
            public ConnectionScopedSqlDistributedLock InternalLock { get; private set; }
            public KeepaliveHelper Keepalive => this.keepalive;

            public void Dispose()
            {
                var keepalive = Interlocked.Exchange(ref this.keepalive, null);
                if (keepalive != null)
                {
                    // begin by stopping the keepalive so we don't have threading issues when disposing the internal handle
                    // try-finally here to make sure we still clean up if people free handles in the wrong order
                    try { keepalive.Stop(); }
                    finally
                    {
                        this.InternalHandle.Dispose();
                        this.InternalHandle = null;
                        this.InternalLock = null;
                        if (!this.ownsKeepalive)
                        {
                            // if the keepalive is owned by an outer handle, restart it
                            keepalive.Start();
                        }
                    }
                }
            }
        }
    }
}
