using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Medallion.Threading.Sql
{
    internal sealed class ExternalConnectionOrTransactionSqlDistributedLocks : IInternalSqlDistributedLocks
    {
        private readonly IEnumerable<string> lockNames;
        private readonly ConnectionOrTransaction connectionOrTransaction;

        public ExternalConnectionOrTransactionSqlDistributedLocks(IEnumerable<string> lockNames, ConnectionOrTransaction connectionOrTransaction)
        {
            this.lockNames = lockNames;
            this.connectionOrTransaction = connectionOrTransaction;
        }

        public IDisposable? TryAcquire<TLockCookie>(int timeoutMillis, ISqlSynchronizationStrategyMultiple<TLockCookie> strategy, IDisposable? contextHandle)
            where TLockCookie : class
        {
            this.CheckConnection();

            var lockCookie = strategy.TryAcquire(this.connectionOrTransaction, this.lockNames, timeoutMillis);
            return this.CreateHandle(strategy, lockCookie);
        }

        private void CheckConnection()
        {
            var connection = this.connectionOrTransaction.Connection;
            if (connection == null) { throw new InvalidOperationException("The transaction had been disposed"); }
            else if (connection.State != ConnectionState.Open) { throw new InvalidOperationException("The connection is not open"); }
        }

        private IDisposable? CreateHandle<TLockCookie>(ISqlSynchronizationStrategyMultiple<TLockCookie> strategy, TLockCookie? lockCookie) where TLockCookie : class
        {
            if (lockCookie == null) { return null; }

            return new ReleaseAction(() =>
            {
                if (this.connectionOrTransaction.Connection?.IsClosedOrBroken() ?? true)
                {
                    // lost the connection or transaction disposed, so the lock was already released
                    return;
                }

                strategy.Release(this.connectionOrTransaction, this.lockNames, lockCookie);
            });
        }
    }
}
