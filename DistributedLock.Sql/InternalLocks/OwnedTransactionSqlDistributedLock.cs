using Medallion.Threading.Internal;
using Microsoft.Data.SqlClient;
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
    internal sealed class OwnedTransactionSqlDistributedLock : IInternalSqlDistributedLock
    {
        private readonly string _name, _connectionString;

        public OwnedTransactionSqlDistributedLock(string name, string connectionString)
        {
            this._name = name;
            this._connectionString = connectionString;
        }

        bool IInternalSqlDistributedLock.IsReentrant => false;

        public async ValueTask<IDistributedLockHandle?> TryAcquireAsync<TLockCookie>(
            TimeoutValue timeout, 
            ISqlSynchronizationStrategy<TLockCookie> strategy, 
            CancellationToken cancellationToken, 
            IDistributedLockHandle? contextHandle)
            where TLockCookie : class
        {
            if (contextHandle != null)
            {
                return await this.CreateContextLock(contextHandle).TryAcquireAsync(timeout, strategy, cancellationToken, contextHandle: null).ConfigureAwait(false);
            }

            IDistributedLockHandle? result = null;
            var connection = new SqlConnection(this._connectionString);
            DbTransaction? transaction = null;
            try
            {
                await SqlHelpers.OpenAsync(connection, cancellationToken).ConfigureAwait(false);
                // when creating a transaction, the isolation level doesn't matter, since we're using sp_getapplock
                transaction = connection.BeginTransaction();
                var lockCookie = await strategy.TryAcquireAsync(transaction, this._name, timeout, cancellationToken).ConfigureAwait(false);
                if (lockCookie != null)
                {
                    result = new Handle(transaction);
                }
            }
            finally
            {
                // if we fail to acquire or throw, make sure to clean up
                if (result == null)
                {
                    if (transaction != null) { await SqlHelpers.DisposeAsync(transaction).ConfigureAwait(false); }
                    await SqlHelpers.DisposeAsync(connection).ConfigureAwait(false);
                }
            }

            return result;
        }

        private IInternalSqlDistributedLock CreateContextLock(IDisposable contextHandle)
        {
            var transaction = ((Handle)contextHandle).Transaction;
            if (transaction == null) { throw new ObjectDisposedException(nameof(contextHandle), "the provided handle is already disposed"); }

            return new ExternalConnectionOrTransactionSqlDistributedLock(this._name, transaction);
        }
        
        private sealed class Handle : IDistributedLockHandle
        {
            private DbTransaction? transaction;

            public Handle(DbTransaction transaction)
            {
                this.transaction = transaction;
            }

            public CancellationToken HandleLostToken => throw new NotImplementedException();

            public DbTransaction? Transaction => Volatile.Read(ref this.transaction);

            public void Dispose() => SyncOverAsync.Run(@this => @this.DisposeAsync(), this, willGoAsync: false);

            public async ValueTask DisposeAsync()
            {
                var transaction = Interlocked.Exchange(ref this.transaction, null);
                if (transaction != null)
                {
                    // todo can we have one dispose method that does this logic for transactions here and above?
                    var connection = transaction.Connection;
                    try
                    {
                        await SqlHelpers.DisposeAsync(transaction).ConfigureAwait(false); // first end the transaction to release the lock
                    }
                    finally
                    {
                        await SqlHelpers.DisposeAsync(connection).ConfigureAwait(false); // then close the connection (returns it to the pool)
                    }
                }
            }
        }
    }
}
