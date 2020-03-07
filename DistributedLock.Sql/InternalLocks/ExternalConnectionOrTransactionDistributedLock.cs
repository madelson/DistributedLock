using Medallion.Threading.Internal;
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
    internal sealed class ExternalConnectionOrTransactionSqlDistributedLock : IInternalSqlDistributedLock
    {
        private readonly string lockName;
        private readonly ConnectionOrTransaction connectionOrTransaction;

        public ExternalConnectionOrTransactionSqlDistributedLock(string lockName, ConnectionOrTransaction connectionOrTransaction)
        {
            this.lockName = lockName;
            this.connectionOrTransaction = connectionOrTransaction;
        }

        bool IInternalSqlDistributedLock.IsReentrant => true;

        public async ValueTask<IDistributedLockHandle?> TryAcquireAsync<TLockCookie>(
            TimeoutValue timeout, 
            ISqlSynchronizationStrategy<TLockCookie> strategy, 
            CancellationToken cancellationToken, 
            IDistributedLockHandle? contextHandle)
            where TLockCookie : class
        {
            this.CheckConnection();

            var lockCookie = await strategy.TryAcquireAsync(this.connectionOrTransaction, this.lockName, timeout, cancellationToken).ConfigureAwait(false);
            return lockCookie == null ? null : new Handle<TLockCookie>(this, strategy, lockCookie);
        }

        private void CheckConnection()
        {
            var connection = this.connectionOrTransaction.Connection;
            if (connection == null) { throw new InvalidOperationException("The transaction had been disposed"); }
            else if (connection.State != ConnectionState.Open) { throw new InvalidOperationException("The connection is not open"); }
        }

        private sealed class Handle<TLockCookie> : IDistributedLockHandle
            where TLockCookie : class
        {
            private ExternalConnectionOrTransactionSqlDistributedLock? _lock;
            private ISqlSynchronizationStrategy<TLockCookie>? _strategy;
            private TLockCookie? _lockCookie;

            public Handle(
                ExternalConnectionOrTransactionSqlDistributedLock @lock,
                ISqlSynchronizationStrategy<TLockCookie> strategy, 
                TLockCookie lockCookie)
            {
                this._lock = @lock;
                this._strategy = strategy;
                this._lockCookie = lockCookie;
            }

            public CancellationToken HandleLostToken => throw new NotImplementedException();

            public void Dispose() => SyncOverAsync.Run(@this => this.DisposeAsync(), this, willGoAsync: false);

            public async ValueTask DisposeAsync()
            {
                var @lock = Interlocked.Exchange(ref this._lock, null);
                if (@lock != null)
                {
                    var strategy = Interlocked.Exchange(ref this._strategy, null)!;
                    var lockCookie = Interlocked.Exchange(ref this._lockCookie, null)!;
                    await strategy.ReleaseAsync(@lock.connectionOrTransaction, @lock.lockName, lockCookie).ConfigureAwait(false);
                }
            }
        }
    }
}
