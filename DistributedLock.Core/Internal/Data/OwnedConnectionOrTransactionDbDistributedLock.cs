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
                // todo revisit how this works with managed finalization. We want to avoid the case where the upgraded handle and the original handle get
                // finalized in the wrong order. Perhaps it would be simpler to make upgradestrategy its own different interface, and then make the idblock return
                // a handle type that might be able to self-upgrade if it has the right internal strategy
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
            private InnerHandle? _innerHandle;
            private IDisposable? _finalizer;

            public Handle(DatabaseConnection connection, IDbSynchronizationStrategy<TLockCookie> strategy, string name, TLockCookie lockCookie, bool useTransaction)
            {
                this._innerHandle = new InnerHandle(connection, strategy, name, lockCookie, useTransaction);
                this._finalizer = ManagedFinalizerQueue.Instance.Register(this, this._innerHandle);
            }

            public CancellationToken HandleLostToken => throw new NotImplementedException();

            public DatabaseConnection? Connection => Volatile.Read(ref this._innerHandle)?.Connection;

            public void Dispose() => SyncOverAsync.Run(@this => @this.DisposeAsync(), this, willGoAsync: false);

            public ValueTask DisposeAsync()
            {
                Interlocked.Exchange(ref this._finalizer, null)?.Dispose();
                return Interlocked.Exchange(ref this._innerHandle, null)?.DisposeAsync() ?? default;
            }

            private sealed class InnerHandle : IAsyncDisposable
            {
                private readonly IDbSynchronizationStrategy<TLockCookie> _strategy;
                private readonly string _name;
                private readonly TLockCookie _lockCookie;
                private readonly bool _useTransaction;

                public InnerHandle(DatabaseConnection connection, IDbSynchronizationStrategy<TLockCookie> strategy, string name, TLockCookie lockCookie, bool useTransaction)
                {
                    this.Connection = connection;
                    this._strategy = strategy;
                    this._name = name;
                    this._lockCookie = lockCookie;
                    this._useTransaction = useTransaction;
                }

                public DatabaseConnection Connection { get; }

                public async ValueTask DisposeAsync()
                {
                    try
                    {
                        // If we're not using a transaction, explicit release is required due to connection pooling. 
                        // For a pooled connection, simply calling Dispose() will not release the lock: it just 
                        // returns the connection to the pool
                        if (!this._useTransaction && this.Connection.CanExecuteQueries)
                        {
                            await this._strategy.ReleaseAsync(this.Connection, this._name, this._lockCookie).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        await this.Connection.DisposeAsync().ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
