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

        public IDisposable? TryAcquire<TLockCookie>(int timeoutMillis, ISqlSynchronizationStrategy<TLockCookie> strategy, IDisposable? contextHandle)
            where TLockCookie : class
        {
            if (contextHandle != null)
            {
                // if we are taking a nested lock, we don't want to start another keepalive on the same connection.
                // However, we do need to stop our current keepalive while we take the nested lock to avoid threading issues
                var lockScope = (LockScope)contextHandle;
                lockScope.Keepalive!.Stop();
                try
                {
                    var internalHandle = lockScope.InternalLock!.TryAcquire(timeoutMillis, strategy, contextHandle: lockScope.InternalHandle);
                    return internalHandle != null
                        ? new LockScope(internalHandle, lockScope.InternalLock, lockScope.Keepalive, connection: null)
                        : null;
                }
                finally
                {
                    // always restart, even if the acquisition fails
                    lockScope.Keepalive.Start();
                }
            }

            var connection = new SqlConnection(this.connectionString);
            LockScope? result = null;
            try
            {
                connection.Open();
                var internalLock = new ExternalConnectionOrTransactionSqlDistributedLock(this.lockName, connection);
                var internalHandle = internalLock.TryAcquire(timeoutMillis, strategy, contextHandle: null);
                if (internalHandle != null)
                {
                    var keepalive = new KeepaliveHelper(connection);
                    keepalive.Start();
                    result = new LockScope(internalHandle, internalLock, keepalive, connection);
                }
            }
            finally
            {
                if (result == null) { connection.Dispose(); }
            }

            return result;
        }

        public async Task<IDisposable?> TryAcquireAsync<TLockCookie>(int timeoutMillis, ISqlSynchronizationStrategy<TLockCookie> strategy, CancellationToken cancellationToken, IDisposable? contextHandle)
            where TLockCookie : class
        {
            if (contextHandle != null)
            {
                cancellationToken.ThrowIfCancellationRequested(); // if already canceled, exit immediately

                // if we are taking a nested lock, we don't want to start another keepalive on the same connection.
                // However, we do need to stop our current keepalive while we take the nested lock to avoid threading issues
                var lockScope = (LockScope)contextHandle;
                await lockScope.Keepalive!.StopAsync().ConfigureAwait(false);
                try
                {
                    var internalHandle = await lockScope.InternalLock!.TryAcquireAsync(timeoutMillis, strategy, cancellationToken, contextHandle: lockScope.InternalHandle).ConfigureAwait(false);
                    return internalHandle != null
                        ? new LockScope(internalHandle, lockScope.InternalLock, lockScope.Keepalive, connection: null)
                        : null;
                }
                finally
                {
                    // always restart, even if the acquisition fails
                    lockScope.Keepalive.Start();
                }
            }

            var connection = new SqlConnection(this.connectionString);
            LockScope? result = null;
            try
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                var internalLock = new ExternalConnectionOrTransactionSqlDistributedLock(this.lockName, connection);
                var internalHandle = await internalLock.TryAcquireAsync(timeoutMillis, strategy, cancellationToken, contextHandle: null).ConfigureAwait(false);
                if (internalHandle != null)
                {
                    var keepalive = new KeepaliveHelper(connection);
                    keepalive.Start();
                    result = new LockScope(internalHandle, internalLock, keepalive, connection);
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
            private KeepaliveHelper? keepalive;
            private SqlConnection? connection;

            public LockScope(
                IDisposable internalHandle, 
                ExternalConnectionOrTransactionSqlDistributedLock internalLock,
                KeepaliveHelper keepalive,
                SqlConnection? connection)
            {
                this.InternalHandle = internalHandle;
                this.InternalLock = internalLock;
                this.keepalive = keepalive;
                this.connection = connection;
            }
            
            public IDisposable? InternalHandle { get; private set; }
            public ExternalConnectionOrTransactionSqlDistributedLock? InternalLock { get; private set; }
            public KeepaliveHelper? Keepalive => this.keepalive;

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
                        this.InternalHandle!.Dispose();
                        this.InternalHandle = null;
                        this.InternalLock = null;
                        if (this.connection != null)
                        {
                            // if we own the connection then dispose it
                            this.connection.Dispose();
                            this.connection = null;
                        }
                        else
                        {
                            // otherwise, the keepalive is owned by an outer handle so restart it
                            keepalive.Start();
                        }
                    }
                }
            }
        }
    }
}
