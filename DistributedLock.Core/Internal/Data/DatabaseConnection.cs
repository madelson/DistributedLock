using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Internal.Data
{
    /// <summary>
    /// Abstraction over <see cref="IDbConnection"/> that abstracts away the varying async support
    /// across platforms, smooths over cancellation behavior, and integrates with <see cref="SyncOverAsync"/>
    /// </summary>
#if DEBUG
    public
#else
    internal
#endif
        abstract class DatabaseConnection : IAsyncDisposable
    {
        private readonly IDbConnection _connection;
        private readonly TimeoutValue _keepaliveCadence;
        private IDbTransaction? _transaction;
        private KeepaliveHelper? _keepaliveHelper;

        protected DatabaseConnection(IDbConnection connection, TimeoutValue keepaliveCadence)
        {
            this._connection = connection;
            this._keepaliveCadence = keepaliveCadence;
        }

        protected DatabaseConnection(IDbTransaction transaction, TimeoutValue keepaliveCadence)
            : this(transaction.Connection, keepaliveCadence)
        {
            if (transaction.Connection == null) { throw new InvalidOperationException("Cannot execute queries against a transaction that has been disposed"); }

            this._transaction = transaction;
        }

        public bool HasTransaction => this._transaction != null;

        internal bool CanExecuteQueries => this._connection.State == ConnectionState.Open && (this._transaction == null || this._transaction.Connection != null);
        internal KeepaliveHelper? KeepaliveHelper { get; }

        public void StartKeepalive()
        {
            if (!this._keepaliveCadence.IsInfinite)
            {
                (this._keepaliveHelper ??= new KeepaliveHelper(this, this._keepaliveCadence)).TryStart();
            }
        }

        public ValueTask StopKeepaliveAsync() =>
            this._keepaliveHelper?.TryStopAsync().ConvertToVoid() ?? default;

        public DatabaseCommand CreateCommand()
        {
            var command = this._connection.CreateCommand();
            command.Transaction = this._transaction;
            return new DatabaseCommand(command, this);
        }

        // note: we could have this return an IAsyncDisposable which would allow you to close the transaction
        // without closing the connection. However, we don't currently have any use-cases for that
#if NETSTANDARD2_0 || NET461
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#endif
        public async ValueTask BeginTransactionAsync()
#pragma warning restore CS1998
        {
            Invariant.Require(this._transaction == null);

            this._transaction =
#if NETSTANDARD2_1
             !SyncOverAsync.IsSynchronous && this._connection is DbConnection dbConnection
                ? await dbConnection.BeginTransactionAsync().ConfigureAwait(false)
                : 
#elif NETSTANDARD2_0 || NET461
#else
            ERROR
#endif
                this._connection.BeginTransaction();
        }

        public async ValueTask OpenAsync(CancellationToken cancellationToken)
        {
            if ((cancellationToken.CanBeCanceled || !SyncOverAsync.IsSynchronous)
                && this._connection is DbConnection dbConnection)
            {
                await dbConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                this._connection.Open();
            }
        }

        public ValueTask CloseAsync() => this.DisposeOrCloseAsync(isDispose: false);
        public ValueTask DisposeAsync() => this.DisposeOrCloseAsync(isDispose: true);

        private async ValueTask DisposeOrCloseAsync(bool isDispose)
        {
            try { await this.StopKeepaliveAsync().ConfigureAwait(false); }
            finally
            {
                try { await this.DisposeTransactionAsync().ConfigureAwait(false); }
                finally
                {
#if NETSTANDARD2_1
                if (!SyncOverAsync.IsSynchronous && this._connection is DbConnection dbConnection)
                {
                    await (isDispose ? dbConnection.DisposeAsync() : dbConnection.CloseAsync().AsValueTask()).ConfigureAwait(false);
                }
                else 
                {
                    SyncDisposeConnection();
                }
#elif NETSTANDARD2_0 || NET461
                    SyncDisposeConnection();
#else
            ERROR
#endif
                }
            }

            void SyncDisposeConnection()
            {
                if (isDispose) { this._connection.Dispose(); }
                else { this._connection.Close(); }
            }
        }

        private ValueTask DisposeTransactionAsync()
        {
#if NETSTANDARD2_1
            if (!SyncOverAsync.IsSynchronous && this._transaction is DbTransaction dbTransaction)
            {
                return dbTransaction.DisposeAsync();
            }
#elif NETSTANDARD2_0 || NET461
#else
            ERROR
#endif

            this._transaction?.Dispose();
            return default;
        }

        protected internal abstract bool IsCommandCancellationException(Exception exception);
    }
}
