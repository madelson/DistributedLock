using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    internal sealed class TransactionScopedSqlDistributedLock : IInternalSqlDistributedLock
    {
        private readonly string lockName;
        private readonly DbTransaction transaction;

        public TransactionScopedSqlDistributedLock(string lockName, DbTransaction transaction)
        {
            this.lockName = lockName;
            this.transaction = transaction;
        }

        public IDisposable TryAcquire(int timeoutMillis)
        {
            this.CheckConnection();

            return SqlApplicationLock.ExecuteAcquireCommand(this.transaction, this.lockName, timeoutMillis)
                ? new LockScope(this)
                : null;
        }

        public async Task<IDisposable> TryAcquireAsync(int timeoutMillis, CancellationToken cancellationToken)
        {
            this.CheckConnection();

            return await SqlApplicationLock.ExecuteAcquireCommandAsync(this.transaction, this.lockName, timeoutMillis, cancellationToken).ConfigureAwait(false)
                ? new LockScope(this)
                : null;
        }

        private void CheckConnection()
        {
            if (this.transaction.Connection == null)
                throw new InvalidOperationException("The transaction had been disposed");
            else if (this.transaction.Connection.State != ConnectionState.Open)
                throw new InvalidOperationException("The connection is not open");
        }

        private void Release()
        {
            if (this.transaction.Connection?.IsClosedOrBroken() ?? true)
            {
                // lost the connection or transaction disposed, so the lock was already released released
                return;
            }

            SqlApplicationLock.ExecuteReleaseCommand(this.transaction, this.lockName);
        }

        private sealed class LockScope : IDisposable
        {
            private TransactionScopedSqlDistributedLock @lock;

            public LockScope(TransactionScopedSqlDistributedLock @lock)
            {
                this.@lock = @lock;
            }

            public void Dispose() => Interlocked.Exchange(ref this.@lock, null)?.Release();
        }
    }
}
