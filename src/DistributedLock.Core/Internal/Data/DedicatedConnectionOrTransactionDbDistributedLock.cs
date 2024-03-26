using System.Data;

namespace Medallion.Threading.Internal.Data;

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
    private readonly bool _transactionScopedIfPossible;
    private readonly TimeoutValue _keepaliveCadence;

    /// <summary>
    /// Constructs an instance using the given EXTERNALLY OWNED <paramref name="externalConnectionFactory"/>.
    /// </summary>
    public DedicatedConnectionOrTransactionDbDistributedLock(string name, Func<DatabaseConnection> externalConnectionFactory)
        // MA: useTransaction:true here is a bit weird. However, in practice this value does not impact the external connection
        // flow so it doesn't matter what the value is.
        : this(name, externalConnectionFactory, useTransaction: true, keepaliveCadence: Timeout.InfiniteTimeSpan)
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
        this._transactionScopedIfPossible = useTransaction;
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
            if (contextHandle != null)
            {
                connection = this.GetContextHandleConnection<TLockCookie>(contextHandle);
            }
            else
            {
                connectionResource = connection = this._connectionFactory();
                if (connection.IsExernallyOwned)
                {
                    if (!connection.CanExecuteQueries)
                    {
                        throw new InvalidOperationException("The connection and/or transaction are disposed or closed");
                    }
                }
                else
                {
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    if (this._transactionScopedIfPossible) // for an internally-owned connection, we must create the transaction
                    {
                        await connection.BeginTransactionAsync().ConfigureAwait(false);
                    }
                }
            }

            var lockCookie = await strategy.TryAcquireAsync(connection, this._name, timeout, cancellationToken).ConfigureAwait(false);
            if (lockCookie != null)
            {
                result = new Handle<TLockCookie>(connection, strategy, this._name, lockCookie, transactionScoped: this._transactionScopedIfPossible && connection.HasTransaction, connectionResource);
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

        public void Dispose() => this.DisposeSyncViaAsync();

        public ValueTask DisposeAsync()
        {
            Interlocked.Exchange(ref this._finalizer, null)?.Dispose();
            return Interlocked.Exchange(ref this._innerHandle, null)?.DisposeAsync() ?? default;
        }

        private sealed class InnerHandle : IAsyncDisposable
        {
            private static readonly object DisposedSentinel = new();

            private readonly IDbSynchronizationStrategy<TLockCookie> _strategy;
            private readonly string _name;
            private readonly TLockCookie _lockCookie;
            private readonly bool _transactionScoped;
            private readonly IAsyncDisposable? _connectionResource;
            private object? _connectionMonitoringHandleOrDisposedSentinel;

            public InnerHandle(
                DatabaseConnection connection, 
                IDbSynchronizationStrategy<TLockCookie> strategy, 
                string name, 
                TLockCookie lockCookie, 
                bool transactionScoped,
                IAsyncDisposable? connectionResource)
            {
                this.Connection = connection;
                this._strategy = strategy;
                this._name = name;
                this._lockCookie = lockCookie;
                this._transactionScoped = transactionScoped;
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
                    // For transaction-scoped locks, we can sometimes skip the explicit release step. This comes up when either
                    // (a) We own the connection and therefore the transaction. In this case we're about to dispose the transaction and release that way
                    // (b) The transaction is dead (e. g. completed or rolled back) in which case the lock has already been released
                    var canSkipExplicitRelease =
                        this._transactionScoped && (!this.Connection.IsExernallyOwned || !this.Connection.CanExecuteQueries);
                    if (!canSkipExplicitRelease)
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
