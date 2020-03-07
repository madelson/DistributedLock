using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Data
{
    internal sealed class ExternalConnectionOrTransactionSqlDistributedLock : IInternalSqlDistributedLock
    {
        private readonly string _name;
        private readonly ConnectionOrTransaction _connectionOrTransaction;

        public ExternalConnectionOrTransactionSqlDistributedLock(string name, ConnectionOrTransaction connectionOrTransaction)
        {
            this._name = name;
            this._connectionOrTransaction = connectionOrTransaction;
        }

        // todo isreentrant tests
        bool IInternalSqlDistributedLock.IsReentrant => true;

        public async ValueTask<IDistributedLockHandle?> TryAcquireAsync<TLockCookie>(
            TimeoutValue timeout, 
            ISqlSynchronizationStrategy<TLockCookie> strategy, 
            CancellationToken cancellationToken, 
            IDistributedLockHandle? contextHandle)
            where TLockCookie : class
        {
            this.CheckConnection();

            var lockCookie = await strategy.TryAcquireAsync(this._connectionOrTransaction, this._name, timeout, cancellationToken).ConfigureAwait(false);
            return lockCookie == null ? null : new Handle<TLockCookie>(this, strategy, lockCookie);
        }

        private void CheckConnection()
        {
            var connection = this._connectionOrTransaction.Connection;
            if (connection == null) { throw new InvalidOperationException("The transaction had been disposed"); }
            else if (connection.State != ConnectionState.Open) { throw new InvalidOperationException("The connection is not open"); }
        }

        private sealed class Handle<TLockCookie> : IDistributedLockHandle
            where TLockCookie : class
        {
            private RefBox<(ExternalConnectionOrTransactionSqlDistributedLock @lock, ISqlSynchronizationStrategy<TLockCookie> strategy, TLockCookie lockCookie)>? _box;

            public Handle(
                ExternalConnectionOrTransactionSqlDistributedLock @lock,
                ISqlSynchronizationStrategy<TLockCookie> strategy, 
                TLockCookie lockCookie)
            {
                this._box = RefBox.Create((@lock, strategy, lockCookie));
            }

            public CancellationToken HandleLostToken => throw new NotImplementedException();

            public void Dispose() => SyncOverAsync.Run(@this => this.DisposeAsync(), this, willGoAsync: false);

            public ValueTask DisposeAsync() => 
                RefBox.TryConsume(ref this._box, out var contents)
                    && !(contents.@lock._connectionOrTransaction.Connection?.IsClosedOrBroken() ?? true)
                    ? contents.strategy.ReleaseAsync(contents.@lock._connectionOrTransaction, contents.@lock._name, contents.lockCookie)
                    : default;
        }
    }
}
