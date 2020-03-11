using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Internal.Data
{
    /// <summary>
    /// Implements <see cref="IDbDistributedLock"/> by giving each lock acquisition a dedicated <see cref="IDbConnection"/>
    /// or <see cref="IDbTransaction"/>
    /// </summary>
#if DEBUG
    public
#else
    internal
#endif
    sealed class OwnedConnectionOrTransactionDbDistributedLock : IDbDistributedLock
    {
        private readonly string _name;
        private readonly Func<DatabaseConnection> _connectionFactory;
        private readonly bool _useTransaction;

        public OwnedConnectionOrTransactionDbDistributedLock(string name, Func<DatabaseConnection> connectionFactory, bool useTransaction)
        {
            this._name = name;
            this._connectionFactory = connectionFactory;
            this._useTransaction = useTransaction;
        }

        public async ValueTask<IDistributedLockHandle?> TryAcquireAsync<TLockCookie>(
            TimeoutValue timeout, 
            IDbSynchronizationStrategy<TLockCookie> strategy, 
            CancellationToken cancellationToken, 
            IDistributedLockHandle? contextHandle)
            where TLockCookie : class
        {
            if (contextHandle != null)
            {
                return await this.CreateContextLock<TLockCookie>(contextHandle).TryAcquireAsync(timeout, strategy, cancellationToken, contextHandle: null).ConfigureAwait(false);
            }

            IDistributedLockHandle? result = null;
            var connection = this._connectionFactory();
            try
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                if (this._useTransaction)
                {
                    await connection.BeginTransactionAsync().ConfigureAwait(false);
                }
                var lockCookie = await strategy.TryAcquireAsync(connection, this._name, timeout, cancellationToken).ConfigureAwait(false);
                if (lockCookie != null)
                {
                    result = new Handle<TLockCookie>(connection, strategy, this._name, lockCookie, this._useTransaction);
                }
            }
            finally
            {
                // if we fail to acquire or throw, make sure to clean up the connection
                if (result == null)
                {
                    await connection.DisposeAsync().ConfigureAwait(false);
                }
            }

            return result;
        }

        private IDbDistributedLock CreateContextLock<TLockCookie>(IDisposable contextHandle)
            where TLockCookie : class
        {
            var connection = ((Handle<TLockCookie>)contextHandle).Connection;
            if (connection == null) { throw new ObjectDisposedException(nameof(contextHandle), "the provided handle is already disposed"); }

            return new ExternalConnectionOrTransactionDbDistributedLock(this._name, connection);
        }
        
        private sealed class Handle<TLockCookie> : IDistributedLockHandle
            where TLockCookie : class
        {
            private readonly string _name;
            private readonly bool _useTransaction;
            private RefBox<(DatabaseConnection connection, IDbSynchronizationStrategy<TLockCookie> strategy, TLockCookie lockCookie)>? _box;

            public Handle(DatabaseConnection connection, IDbSynchronizationStrategy<TLockCookie> strategy, string name, TLockCookie lockCookie, bool useTransaction)
            {
                this._box = RefBox.Create((connection, strategy, lockCookie));
                this._name = name;
                this._useTransaction = useTransaction;
            }

            public CancellationToken HandleLostToken => throw new NotImplementedException();

            public DatabaseConnection? Connection => Volatile.Read(ref this._box)?.Value.connection;

            public void Dispose() => SyncOverAsync.Run(@this => @this.DisposeAsync(), this, willGoAsync: false);

            public async ValueTask DisposeAsync()
            {
                if (RefBox.TryConsume(ref this._box, out var contents))
                {
                    try
                    {
                        // If we're not using a transaction, explicit release is required due to connection pooling. 
                        // For a pooled connection, simply calling Dispose() will not release the lock: it just 
                        // returns the connection to the pool
                        if (!this._useTransaction && contents.connection.CanExecuteQueries)
                        {
                            await contents.strategy.ReleaseAsync(contents.connection, this._name, contents.lockCookie).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        await contents.connection.DisposeAsync().ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
