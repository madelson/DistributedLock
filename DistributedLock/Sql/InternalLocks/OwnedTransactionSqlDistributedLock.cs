using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    internal sealed class OwnedTransactionSqlDistributedLock : IInternalSqlDistributedLock
    {
        private readonly string lockName, connectionString;

        public OwnedTransactionSqlDistributedLock(string lockName, string connectionString)
        {
            this.lockName = lockName;
            this.connectionString = connectionString;
        }

        public IDisposable TryAcquire(int timeoutMillis)
        {
            IDisposable result = null;
            var connection = new SqlConnection(this.connectionString);
            SqlTransaction transaction = null;
            try
            {
                connection.Open();
                // when creating a transaction, the isolation level doesn't matter, since we're using sp_getapplock
                transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted);
                if (SqlApplicationLock.ExecuteAcquireCommand(transaction, this.lockName, timeoutMillis))
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

        public async Task<IDisposable> TryAcquireAsync(int timeoutMillis, CancellationToken cancellationToken)
        {
            IDisposable result = null;
            var connection = new SqlConnection(this.connectionString);
            SqlTransaction transaction = null;
            try
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                // when creating a transaction, the isolation level doesn't matter, since we're using sp_getapplock
                transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted);
                if (await SqlApplicationLock.ExecuteAcquireCommandAsync(transaction, this.lockName, timeoutMillis, cancellationToken).ConfigureAwait(false))
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

        private sealed class LockScope : IDisposable
        {
            private SqlTransaction transaction;

            public LockScope(SqlTransaction transaction)
            {
                this.transaction = transaction;
            }

            public void Dispose()
            {
                var transaction = Interlocked.Exchange(ref this.transaction, null);
                if (transaction != null)
                {
                    var connection = transaction.Connection;
                    transaction.Dispose(); // first end the transaction to release the lock
                    connection.Dispose(); // then close the connection (returns it to the pool)
                }
            }
        }
    }
}
