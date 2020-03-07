using Medallion.Threading.Internal;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Data
{
    internal sealed class AzureSqlDistributedLock : IInternalSqlDistributedLock
    {
        private readonly string _name, _connectionString;

        public AzureSqlDistributedLock(string lockName, string connectionString)
        {
            this._name = lockName;
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
                cancellationToken.ThrowIfCancellationRequested(); // if already canceled, exit immediately

                // if we are taking a nested lock, we don't want to start another keepalive on the same connection.
                // However, we do need to stop our current keepalive while we take the nested lock to avoid threading issues
                var lockScope = (Handle)contextHandle;
                await lockScope.Keepalive!.StopAsync().ConfigureAwait(false);
                try
                {
                    var internalHandle = await lockScope.InternalLock!.TryAcquireAsync(timeout, strategy, cancellationToken, contextHandle: lockScope.InternalHandle).ConfigureAwait(false);
                    return internalHandle != null
                        ? new Handle(internalHandle, lockScope.InternalLock, lockScope.Keepalive, connection: null)
                        : null;
                }
                finally
                {
                    // always restart, even if the acquisition fails
                    lockScope.Keepalive.Start();
                }
            }

            var connection = new SqlConnection(this._connectionString);
            Handle? result = null;
            try
            {
                await SqlHelpers.OpenAsync(connection, cancellationToken).ConfigureAwait(false);
                var internalLock = new ExternalConnectionOrTransactionSqlDistributedLock(this._name, connection);
                var internalHandle = await internalLock.TryAcquireAsync(timeout, strategy, cancellationToken, contextHandle: null).ConfigureAwait(false);
                if (internalHandle != null)
                {
                    var keepalive = new KeepaliveHelper(connection);
                    keepalive.Start();
                    result = new Handle(internalHandle, internalLock, keepalive, connection);
                }
            }
            finally
            {
                if (result == null) { await SqlHelpers.DisposeAsync(connection).ConfigureAwait(false); }
            }

            return result;
        }
        
        private sealed class Handle : IDistributedLockHandle
        {
            private KeepaliveHelper? _keepalive;
            private DbConnection? _connection;

            public Handle(
                IDistributedLockHandle internalHandle, 
                ExternalConnectionOrTransactionSqlDistributedLock internalLock,
                KeepaliveHelper keepalive,
                DbConnection? connection)
            {
                this.InternalHandle = internalHandle;
                this.InternalLock = internalLock;
                this._keepalive = keepalive;
                this._connection = connection;
            }
            
            public IDistributedLockHandle? InternalHandle { get; private set; }
            public ExternalConnectionOrTransactionSqlDistributedLock? InternalLock { get; private set; }
            public KeepaliveHelper? Keepalive => this._keepalive;

            public CancellationToken HandleLostToken => throw new NotImplementedException();

            void IDisposable.Dispose() => SyncOverAsync.Run(@this => @this.DisposeAsync(), this, willGoAsync: false);

            public async ValueTask DisposeAsync()
            {
                var keepalive = Interlocked.Exchange(ref this._keepalive, null);
                if (keepalive != null)
                {
                    // begin by stopping the keepalive so we don't have threading issues when disposing the internal handle
                    // try-finally here to make sure we still clean up if people free handles in the wrong order
                    try { await keepalive.StopAsync().ConfigureAwait(false); }
                    finally
                    {
                        // todo revisit ordering of nulling out (probably for all handles)
                        await this.InternalHandle!.DisposeAsync().ConfigureAwait(false);
                        this.InternalHandle = null;
                        this.InternalLock = null;
                        if (this._connection != null)
                        {
                            // if we own the connection then dispose it
                            await SqlHelpers.DisposeAsync(this._connection).ConfigureAwait(false);
                            this._connection = null;
                        }
                        else
                        {
                            // otherwise, the keepalive is owned by an outer handle so restart it
                            keepalive.Start();
                        }
                    }
                }
            }
        }
    }
}
