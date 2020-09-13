using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace Medallion.Threading.Sql
{
    internal sealed class OwnedTransactionSqlDistributedLocks : IInternalSqlDistributedLocks
    {
        private readonly IEnumerable<string> lockNames;
        private readonly string connectionString;

        public OwnedTransactionSqlDistributedLocks(IEnumerable<string> lockNames, string connectionString)
        {
            this.lockNames = lockNames;
            this.connectionString = connectionString;
        }

        public IDisposable? TryAcquire<TLockCookie>(int timeoutMillis, ISqlSynchronizationStrategyMultiple<TLockCookie> strategy, IDisposable? contextHandle)
            where TLockCookie : class
        {
            if (contextHandle != null)
            {
                return this.CreateContextLock(contextHandle).TryAcquire(timeoutMillis, strategy, contextHandle: null);
            }

            IDisposable? result = null;
            var connection = SqlHelpers.CreateConnection(this.connectionString);
            DbTransaction? transaction = null;
            try
            {
                connection.Open();
                // when creating a transaction, the isolation level doesn't matter, since we're using sp_getapplock
                transaction = connection.BeginTransaction();
                var lockCookie = strategy.TryAcquire(transaction, this.lockNames, timeoutMillis);
                if (lockCookie != null)
                {
                    result = new LockScope(transaction);
                }
            }
            finally
            {
                // if we fail to acquire or throw, make sure to clean up
                if (result == null)
                {
                    transaction?.Dispose();
                    connection.Dispose();
                }
            }

            return result;
        }
        
        private IInternalSqlDistributedLocks CreateContextLock(IDisposable contextHandle)
        {
            var transaction = ((LockScope)contextHandle).Transaction;
            if (transaction == null) { throw new ObjectDisposedException(nameof(contextHandle), "the provided handle is already disposed"); }

            return new ExternalConnectionOrTransactionSqlDistributedLocks(this.lockNames, transaction);
        }
        
        private sealed class LockScope : IDisposable
        {
            private DbTransaction? transaction;

            public LockScope(DbTransaction transaction)
            {
                this.transaction = transaction;
            }

            public DbTransaction? Transaction => Volatile.Read(ref this.transaction);

            public void Dispose()
            {
                var localTransaction = Interlocked.Exchange(ref this.transaction, null);
                if (localTransaction != null)
                {
                    var connection = localTransaction.Connection;
                    localTransaction.Dispose(); // first end the transaction to release the lock
                    connection.Dispose(); // then close the connection (returns it to the pool)
                }
            }
        }
    }
}
