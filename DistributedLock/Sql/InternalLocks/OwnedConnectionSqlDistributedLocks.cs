using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace Medallion.Threading.Sql
{
    internal sealed class OwnedConnectionSqlDistributedLocks : IInternalSqlDistributedLocks
    {
        private readonly IEnumerable<string> lockNames;
        private readonly string connectionString;

        public OwnedConnectionSqlDistributedLocks(IEnumerable<string> lockNames, string connectionString)
        {
            this.lockNames = lockNames;
            this.connectionString = connectionString;
        }

        public IDisposable? TryAcquire<TLockCookie>(int timeoutMillis, ISqlSynchronizationStrategyMultiple<TLockCookie> strategy, IDisposable? contextHandle)
            where TLockCookie : class
        {
            if (contextHandle != null)
            {
                return this.CreateContextLock<TLockCookie>(contextHandle).TryAcquire(timeoutMillis, strategy, contextHandle: null);
            }

            IDisposable? result = null;
            var connection = SqlHelpers.CreateConnection(this.connectionString);
            try
            {
                connection.Open();
                var lockCookie = strategy.TryAcquire(connection, this.lockNames, timeoutMillis);
                if (lockCookie != null)
                {
                    result = new LockScope<TLockCookie>(connection, strategy, this.lockNames, lockCookie);
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
        
        private IInternalSqlDistributedLocks CreateContextLock<TLockCookie>(IDisposable contextHandle)
            where TLockCookie : class
        {
            var connection = ((LockScope<TLockCookie>)contextHandle).Connection;
            if (connection == null) { throw new ObjectDisposedException(nameof(contextHandle), "the provided handle is already disposed"); }

            return new ExternalConnectionOrTransactionSqlDistributedLocks(this.lockNames, connection);
        }
        
        private sealed class LockScope<TLockCookie> : IDisposable
            where TLockCookie : class
        {
            private DbConnection? connection;
            private readonly IEnumerable<string> lockNames;
            private ISqlSynchronizationStrategyMultiple<TLockCookie>? strategy;
            private TLockCookie? lockCookie;

            public LockScope(DbConnection connection, ISqlSynchronizationStrategyMultiple<TLockCookie> strategy, IEnumerable<string> lockNames, TLockCookie lockCookie)
            {
                this.connection = connection;
                this.strategy = strategy;
                this.lockNames = lockNames;
                this.lockCookie = lockCookie;
            }

            public DbConnection? Connection => Volatile.Read(ref this.connection);

            public void Dispose()
            {
                var localConnection = Interlocked.Exchange(ref this.connection, null);
                if (localConnection != null && !localConnection.IsClosedOrBroken())
                {
                    ReleaseLock(localConnection, this.strategy!, this.lockNames, this.lockCookie!);
                    this.strategy = null;
                    this.lockCookie = null;
                }
            }

            private static void ReleaseLock(DbConnection connection, ISqlSynchronizationStrategyMultiple<TLockCookie> strategy, IEnumerable<string> lockNames, TLockCookie lockCookie)
            {
                try
                {
                    // explicit release is required due to connection pooling. For a pooled connection,
                    // simply calling Dispose() will not release the lock: it just returns the connection
                    // to the pool
                    strategy.Release(connection, lockNames, lockCookie);
                }
                finally
                {
                    connection.Dispose();
                }
            }
        }
    }
}
