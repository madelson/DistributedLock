using System;
using System.Data;
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
    sealed class DedicatedConnectionOrTransactionDbDistributedLock : IDbDistributedLock
    {
        private readonly string _name;
        private readonly Func<DatabaseConnection> _connectionFactory;
        private readonly bool _scopeToOwnedTransaction;
        private readonly TimeoutValue _keepaliveCadence;

        public DedicatedConnectionOrTransactionDbDistributedLock(string name, Func<DatabaseConnection> externalConnectionFactory)
            : this(name, externalConnectionFactory, useTransaction: false, keepaliveCadence: Timeout.InfiniteTimeSpan)
        {
        }

        public DedicatedConnectionOrTransactionDbDistributedLock(
            string name, 
            Func<DatabaseConnection> connectionFactory, 
            bool useTransaction,
            TimeoutValue keepaliveCadence)
        {
            this._name = name;
            this._connectionFactory = connectionFactory;
            this._scopeToOwnedTransaction = useTransaction;
            this._keepaliveCadence = keepaliveCadence;
        }

        public async ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync<TLockCookie>(
            TimeoutValue timeout, 
            IDbSynchronizationStrategy<TLockCookie> strategy, 
            CancellationToken cancellationToken, 
            IDistributedSynchronizationHandle? contextHandle)
            where TLockCookie : class
        {
            IDistributedSynchronizationHandle? result = null;
            IAsyncDisposable? connectionResource = null;
            try
            {
                DatabaseConnection connection;
                bool transactionScoped;
                if (contextHandle != null)
                {
                    connection = GetContextHandleConnection<TLockCookie>(contextHandle);
                    transactionScoped = false;
                }
                else
                {
                    connectionResource = connection = this._connectionFactory();
                    if (connection.IsExernallyOwned)
                    {
                        Invariant.Require(!this._scopeToOwnedTransaction);
                        if (!connection.CanExecuteQueries)
                        {
                            throw new InvalidOperationException("The connection and/or transaction are disposed or closed");
                        }
                        transactionScoped = false;
                    }
                    else
                    {
                        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                        if (this._scopeToOwnedTransaction)
                        {
                            await connection.BeginTransactionAsync().ConfigureAwait(false);
                        }
                        transactionScoped = this._scopeToOwnedTransaction;
                    }
                }

                var lockCookie = await strategy.TryAcquireAsync(connection, this._name, timeout, cancellationToken).ConfigureAwait(false);
                if (lockCookie != null)
                {
                    result = new Handle<TLockCookie>(connection, strategy, this._name, lockCookie, transactionScoped, connectionResource);
                    if (!this._keepaliveCadence.IsInfinite)
                    {
                        connection.SetKeepaliveCadence(this._keepaliveCadence);
                    }
                }
            }
            finally
            {
                // if we fail to acquire or throw, make sure to clean up the connection
                if (result == null && connectionResource != null)
                {
                    await connectionResource.DisposeAsync().ConfigureAwait(false);
                }
            }

            return result;
        }

        private DatabaseConnection GetContextHandleConnection<TLockCookie>(IDistributedSynchronizationHandle contextHandle)
            where TLockCookie : class
        {
            var connection = ((Handle<TLockCookie>)contextHandle).Connection;
            if (connection == null) { throw new ObjectDisposedException(nameof(contextHandle), "the provided handle is already disposed"); }
            return connection;
        }

        private sealed class Handle<TLockCookie> : IDistributedSynchronizationHandle
            where TLockCookie : class
        {
            private InnerHandle? _innerHandle;
            private IDisposable? _finalizer;

            public Handle(
                DatabaseConnection connection, 
                IDbSynchronizationStrategy<TLockCookie> strategy, 
                string name, 
                TLockCookie lockCookie, 
                bool transactionScoped,
                IAsyncDisposable? connectionResource)
            {
                this._innerHandle = new InnerHandle(connection, strategy, name, lockCookie, transactionScoped, connectionResource);
                // we don't do managed finalization for externally-owned connections/transactions since it might violate thread-safety
                // on those objects (we don't know when they're in use)
                this._finalizer = connection.IsExernallyOwned ? null : ManagedFinalizerQueue.Instance.Register(this, this._innerHandle);
            }

            public CancellationToken HandleLostToken => Volatile.Read(ref this._innerHandle)?.HandleLostToken ?? throw this.ObjectDisposed();

            public DatabaseConnection? Connection => Volatile.Read(ref this._innerHandle)?.Connection;

            public void Dispose() => SyncOverAsync.Run(@this => @this.DisposeAsync(), this);

            public ValueTask DisposeAsync()
            {
                Interlocked.Exchange(ref this._finalizer, null)?.Dispose();
                return Interlocked.Exchange(ref this._innerHandle, null)?.DisposeAsync() ?? default;
            }

            private sealed class InnerHandle : IAsyncDisposable
            {
                private static readonly object DisposedSentinel = new object();

                private readonly IDbSynchronizationStrategy<TLockCookie> _strategy;
                private readonly string _name;
                private readonly TLockCookie _lockCookie;
                private readonly bool _scopedToOwnedTransaction;
                private readonly IAsyncDisposable? _connectionResource;
                private object? _connectionMonitoringHandleOrDisposedSentinel;

                public InnerHandle(
                    DatabaseConnection connection, 
                    IDbSynchronizationStrategy<TLockCookie> strategy, 
                    string name, 
                    TLockCookie lockCookie, 
                    bool scopedToOwnTransaction,
                    IAsyncDisposable? connectionResource)
                {
                    this.Connection = connection;
                    this._strategy = strategy;
                    this._name = name;
                    this._lockCookie = lockCookie;
                    this._scopedToOwnedTransaction = scopedToOwnTransaction;
                    this._connectionResource = connectionResource;
                }

                public DatabaseConnection Connection { get; }

                public CancellationToken HandleLostToken
                {
                    get
                    {
                        var existing = Volatile.Read(ref this._connectionMonitoringHandleOrDisposedSentinel);
                        
                        // if we don't have a handle and aren't disposed, try to make a handle
                        if (existing == null)
                        {
                            // tentatively create a new handle and try to assign it
                            var newHandle = this.Connection.GetConnectionMonitoringHandle();
                            existing = Interlocked.CompareExchange(ref this._connectionMonitoringHandleOrDisposedSentinel, newHandle, comparand: null);
                            
                            if (existing == null)
                            {
                                // we won the race: use our new handle
                                return newHandle.ConnectionLostToken;
                            }

                            // We lost the race: discard our new handle. 
                            // Existing is now either a handle created in a race with us or the disposed sentinel
                            newHandle.Dispose();
                        }

                        if (existing == DisposedSentinel)
                        {
                            throw this.ObjectDisposed();
                        }

                        return ((IDatabaseConnectionMonitoringHandle)existing).ConnectionLostToken;
                    }
                }

                public async ValueTask DisposeAsync()
                {
                    var connectionMonitoringHandleOrDisposedSentinel = Interlocked.Exchange(ref this._connectionMonitoringHandleOrDisposedSentinel, DisposedSentinel);
                    if (connectionMonitoringHandleOrDisposedSentinel == DisposedSentinel) { return; }

                    if (connectionMonitoringHandleOrDisposedSentinel is IDatabaseConnectionMonitoringHandle handle)
                    {
                        handle.Dispose();
                    }

                    try
                    {
                        // If we're not scoped to a transaction, explicit release is required regardless of whether
                        // we are about to dispose the connection due to connection pooling. For a pooled connection, 
                        // simply calling Dispose() will not release the lock: it just returns the connection to the pool.
                        if (!(this._scopedToOwnedTransaction 
                                // For external transaction-scoped locks, we're not about to dispose the transaction but if the transaction is
                                // dead (e. g. completed or rolled back) then we know the lock has been released.
                                || (this.Connection.IsExernallyOwned && this.Connection.HasTransaction && !this.Connection.CanExecuteQueries)))
                        {
                            await this._strategy.ReleaseAsync(this.Connection, this._name, this._lockCookie).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        await (this._connectionResource?.DisposeAsync() ?? default).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
