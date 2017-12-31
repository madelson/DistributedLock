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
    // todo can we combine this with ConnectionScoped to make ConnectionOrTransactionScoped?
    internal sealed class TransactionScopedSqlDistributedLock : IInternalSqlDistributedLock
    {
        private readonly string lockName;
        private readonly IDbTransaction transaction;

        public TransactionScopedSqlDistributedLock(string lockName, IDbTransaction transaction)
        {
            this.lockName = lockName;
            this.transaction = transaction;
        }

        public IDisposable TryAcquire<TLockCookie>(int timeoutMillis, ISqlSynchronizationStrategy<TLockCookie> strategy, IDisposable contextHandle)
            where TLockCookie : class
        {
            this.CheckConnection();

            var lockCookie = strategy.TryAcquire(new ConnectionOrTransaction(this.transaction), this.lockName, timeoutMillis);
            return lockCookie != null ? new LockScope<TLockCookie>(this, strategy, lockCookie) : null;
        }

        public async Task<IDisposable> TryAcquireAsync<TLockCookie>(int timeoutMillis, ISqlSynchronizationStrategy<TLockCookie> strategy, CancellationToken cancellationToken, IDisposable contextHandle)
            where TLockCookie : class
        {
            this.CheckConnection();

            var lockCookie = await strategy.TryAcquireAsync(new ConnectionOrTransaction(this.transaction), this.lockName, timeoutMillis, cancellationToken).ConfigureAwait(false);
            return lockCookie != null ? new LockScope<TLockCookie>(this, strategy, lockCookie) : null;
        }

        private void CheckConnection()
        {
            if (this.transaction.Connection == null)
                throw new InvalidOperationException("The transaction had been disposed");
            else if (this.transaction.Connection.State != ConnectionState.Open)
                throw new InvalidOperationException("The connection is not open");
        }

        private void Release<TLockCookie>(ISqlSynchronizationStrategy<TLockCookie> strategy, TLockCookie lockCookie)
            where TLockCookie : class
        {
            if (this.transaction.Connection?.IsClosedOrBroken() ?? true)
            {
                // lost the connection or transaction disposed, so the lock was already released released
                return;
            }

            strategy.Release(new ConnectionOrTransaction(this.transaction), this.lockName, lockCookie);
        }

        private sealed class LockScope<TLockCookie> : IDisposable
            where TLockCookie : class
        {
            private TransactionScopedSqlDistributedLock @lock;
            private ISqlSynchronizationStrategy<TLockCookie> strategy;
            private TLockCookie lockCookie;

            public LockScope(TransactionScopedSqlDistributedLock @lock, ISqlSynchronizationStrategy<TLockCookie> strategy, TLockCookie lockCookie)
            {
                this.@lock = @lock;
                this.strategy = strategy;
                this.lockCookie = lockCookie;
            }

            public void Dispose()
            {
                var @lock = Interlocked.Exchange(ref this.@lock, null);
                if (@lock != null)
                {
                    @lock.Release(this.strategy, this.lockCookie);
                    this.strategy = null;
                    this.lockCookie = null;
                }
            }
        }
    }
}
