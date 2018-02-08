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
    internal sealed class ExternalConnectionOrTransactionSqlDistributedLock : IInternalSqlDistributedLock
    {
        private readonly string lockName;
        private readonly ConnectionOrTransaction connectionOrTransaction;

        public ExternalConnectionOrTransactionSqlDistributedLock(string lockName, ConnectionOrTransaction connectionOrTransaction)
        {
            this.lockName = lockName;
            this.connectionOrTransaction = connectionOrTransaction;
        }

        public IDisposable TryAcquire<TLockCookie>(int timeoutMillis, ISqlSynchronizationStrategy<TLockCookie> strategy, IDisposable contextHandle)
            where TLockCookie : class
        {
            this.CheckConnection();

            var lockCookie = strategy.TryAcquire(this.connectionOrTransaction, this.lockName, timeoutMillis);
            return this.CreateHandle(strategy, lockCookie);
        }

        public async Task<IDisposable> TryAcquireAsync<TLockCookie>(int timeoutMillis, ISqlSynchronizationStrategy<TLockCookie> strategy, CancellationToken cancellationToken, IDisposable contextHandle)
            where TLockCookie : class
        {
            this.CheckConnection();

            var lockCookie = await strategy.TryAcquireAsync(this.connectionOrTransaction, this.lockName, timeoutMillis, cancellationToken).ConfigureAwait(false);
            return this.CreateHandle(strategy, lockCookie);
        }

        private void CheckConnection()
        {
            var connection = this.connectionOrTransaction.Connection;
            if (connection == null) { throw new InvalidOperationException("The transaction had been disposed"); }
            else if (connection.State != ConnectionState.Open) { throw new InvalidOperationException("The connection is not open"); }
        }

        private IDisposable CreateHandle<TLockCookie>(ISqlSynchronizationStrategy<TLockCookie> strategy, TLockCookie lockCookie) where TLockCookie : class
        {
            if (lockCookie == null) { return null; }

            return new ReleaseAction(() =>
            {
                if (this.connectionOrTransaction.Connection?.IsClosedOrBroken() ?? true)
                {
                    // lost the connection or transaction disposed, so the lock was already released
                    return;
                }

                strategy.Release(this.connectionOrTransaction, this.lockName, lockCookie);
            });
        }
    }
}
