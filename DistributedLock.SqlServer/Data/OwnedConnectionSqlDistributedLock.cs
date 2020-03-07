using Medallion.Threading.Internal;
using Microsoft.Data.SqlClient;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Data
{
    internal sealed class OwnedConnectionSqlDistributedLock : IInternalSqlDistributedLock
    {
        private readonly string _name, _connectionString;

        public OwnedConnectionSqlDistributedLock(string name, string connectionString)
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
                return await this.CreateContextLock<TLockCookie>(contextHandle).TryAcquireAsync(timeout, strategy, cancellationToken, contextHandle: null).ConfigureAwait(false);
            }

            IDistributedLockHandle? result = null;
            var connection = new SqlConnection(this._connectionString);
            try
            {
                await SqlHelpers.OpenAsync(connection, cancellationToken).ConfigureAwait(false);
                var lockCookie = await strategy.TryAcquireAsync(connection, this._name, timeout, cancellationToken).ConfigureAwait(false);
                if (lockCookie != null)
                {
                    result = new Handle<TLockCookie>(connection, strategy, this._name, lockCookie);
                }
            }
            finally
            {
                // if we fail to acquire or throw, make sure to clean up the connection
                if (result == null)
                {
                    await SqlHelpers.DisposeAsync(connection).ConfigureAwait(false);
                }
            }

            return result;
        }

        private IInternalSqlDistributedLock CreateContextLock<TLockCookie>(IDisposable contextHandle)
            where TLockCookie : class
        {
            var connection = ((Handle<TLockCookie>)contextHandle).Connection;
            if (connection == null) { throw new ObjectDisposedException(nameof(contextHandle), "the provided handle is already disposed"); }

            return new ExternalConnectionOrTransactionSqlDistributedLock(this._name, connection);
        }
        
        private sealed class Handle<TLockCookie> : IDistributedLockHandle
            where TLockCookie : class
        {
            private RefBox<(DbConnection connection, ISqlSynchronizationStrategy<TLockCookie> strategy, string name, TLockCookie lockCookie)>? _box;

            public Handle(DbConnection connection, ISqlSynchronizationStrategy<TLockCookie> strategy, string name, TLockCookie lockCookie)
            {
                this._box = RefBox.Create((connection, strategy, name, lockCookie));
            }

            public CancellationToken HandleLostToken => throw new NotImplementedException();

            public DbConnection? Connection => Volatile.Read(ref this._box)?.Value.connection;

            public void Dispose() => SyncOverAsync.Run(@this => @this.DisposeAsync(), this, willGoAsync: false);

            public async ValueTask DisposeAsync()
            {
                if (RefBox.TryConsume(ref this._box, out var contents)
                    && !contents.connection.IsClosedOrBroken())
                {
                    try
                    {
                        // explicit release is required due to connection pooling. For a pooled connection,
                        // simply calling Dispose() will not release the lock: it just returns the connection
                        // to the pool
                        await contents.strategy.ReleaseAsync(contents.connection, contents.name, contents.lockCookie).ConfigureAwait(false);
                    }
                    finally
                    {
                        await SqlHelpers.DisposeAsync(contents.connection).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
