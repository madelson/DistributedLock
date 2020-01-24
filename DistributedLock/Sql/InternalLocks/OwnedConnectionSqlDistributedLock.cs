using Microsoft.Data.SqlClient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    internal sealed class OwnedConnectionSqlDistributedLock : IInternalSqlDistributedLock
    {
        private readonly string lockName, connectionString;

        public OwnedConnectionSqlDistributedLock(string lockName, string connectionString)
        {
            this.lockName = lockName;
            this.connectionString = connectionString;
        }

        public IDisposable TryAcquire<TLockCookie>(int timeoutMillis, ISqlSynchronizationStrategy<TLockCookie> strategy, IDisposable contextHandle)
            where TLockCookie : class
        {
            if (contextHandle != null)
            {
                return this.CreateContextLock<TLockCookie>(contextHandle).TryAcquire(timeoutMillis, strategy, contextHandle: null);
            }

            IDisposable result = null;
            var connection = new SqlConnection(this.connectionString);
            try
            {
                connection.Open();
                var lockCookie = strategy.TryAcquire(connection, this.lockName, timeoutMillis);
                if (lockCookie != null)
                {
                    result = new LockScope<TLockCookie>(connection, strategy, this.lockName, lockCookie);
                }    
            }
            finally
            {
                // if we fail to acquire or throw, make sure to clean up the connection
                if (result == null)
                {
                    connection.Dispose();
                }
            }

            return result;
        }

        public async Task<IDisposable> TryAcquireAsync<TLockCookie>(int timeoutMillis, ISqlSynchronizationStrategy<TLockCookie> strategy, CancellationToken cancellationToken, IDisposable contextHandle)
            where TLockCookie : class
        {
            if (contextHandle != null)
            {
                return await this.CreateContextLock<TLockCookie>(contextHandle).TryAcquireAsync(timeoutMillis, strategy, cancellationToken, contextHandle: null).ConfigureAwait(false);
            }

            IDisposable result = null;
            var connection = new SqlConnection(this.connectionString);
            try
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                var lockCookie = await strategy.TryAcquireAsync(connection, this.lockName, timeoutMillis, cancellationToken).ConfigureAwait(false);
                if (lockCookie != null)
                {
                    result = new LockScope<TLockCookie>(connection, strategy, this.lockName, lockCookie);
                }
            }
            finally
            {
                // if we fail to acquire or throw, make sure to clean up the connection
                if (result == null)
                {
                    connection.Dispose();
                }
            }

            return result;
        }

        private IInternalSqlDistributedLock CreateContextLock<TLockCookie>(IDisposable contextHandle)
            where TLockCookie : class
        {
            var connection = ((LockScope<TLockCookie>)contextHandle).Connection;
            if (connection == null) { throw new ObjectDisposedException(nameof(contextHandle), "the provided handle is already disposed"); }

            return new ExternalConnectionOrTransactionSqlDistributedLock(this.lockName, connection);
        }
        
        private sealed class LockScope<TLockCookie> : IDisposable
            where TLockCookie : class
        {
            private SqlConnection connection;
            private readonly string lockName;
            private ISqlSynchronizationStrategy<TLockCookie> strategy;
            private TLockCookie lockCookie;

            public LockScope(SqlConnection connection, ISqlSynchronizationStrategy<TLockCookie> strategy, string lockName, TLockCookie lockCookie)
            {
                this.connection = connection;
                this.strategy = strategy;
                this.lockName = lockName;
                this.lockCookie = lockCookie;
            }

            public SqlConnection Connection => Volatile.Read(ref this.connection);

            public void Dispose()
            {
                var connection = Interlocked.Exchange(ref this.connection, null);
                if (connection != null && !connection.IsClosedOrBroken())
                {
                    ReleaseLock(connection, this.strategy, this.lockName, this.lockCookie);
                    this.strategy = null;
                    this.lockCookie = null;
                }
            }

            private static void ReleaseLock(SqlConnection connection, ISqlSynchronizationStrategy<TLockCookie> strategy, string lockName, TLockCookie lockCookie)
            {
                try
                {
                    // explicit release is required due to connection pooling. For a pooled connection,
                    // simply calling Dispose() will not release the lock: it just returns the connection
                    // to the pool
                    strategy.Release(connection, lockName, lockCookie);
                }
                finally
                {
                    connection.Dispose();
                }
            }
        }
    }
}
