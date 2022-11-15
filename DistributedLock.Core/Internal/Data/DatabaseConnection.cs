using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Internal.Data;

/// <summary>
/// Abstraction over <see cref="IDbConnection"/> that abstracts away the varying async support
/// across platforms, smooths over cancellation behavior, and integrates with <see cref="SyncViaAsync"/>
/// </summary>
#if DEBUG
public
#else
internal
#endif
    abstract class DatabaseConnection : IAsyncDisposable
{
    private IDbTransaction? _transaction;

    protected DatabaseConnection(IDbConnection connection, bool isExternallyOwned)
    {
        this.InnerConnection = connection;
        this.IsExernallyOwned = isExternallyOwned;
        this.ConnectionMonitor = new ConnectionMonitor(this);
    }

    protected DatabaseConnection(IDbTransaction transaction, bool isExternallyOwned)
        : this(transaction.Connection ?? throw new InvalidOperationException("Cannot execute queries against a transaction that has been disposed"), isExternallyOwned)
    {
        this._transaction = transaction;
    }

    internal ConnectionMonitor ConnectionMonitor { get; }
    internal IDbConnection InnerConnection { get; }

    public bool HasTransaction => this._transaction != null;
    
    public bool IsExernallyOwned { get; }

    public abstract bool ShouldPrepareCommands { get; }

    internal bool CanExecuteQueries => this.InnerConnection.State == ConnectionState.Open && (this._transaction == null || this._transaction.Connection != null);
    
    internal void SetKeepaliveCadence(TimeoutValue cadence) => this.ConnectionMonitor.SetKeepaliveCadence(cadence);

    internal IDatabaseConnectionMonitoringHandle GetConnectionMonitoringHandle() => this.ConnectionMonitor.GetMonitoringHandle();

    public DatabaseCommand CreateCommand()
    {
        var command = this.InnerConnection.CreateCommand();
        command.Transaction = this._transaction;
        return new DatabaseCommand(command, this);
    }

    // note: we could have this return an IAsyncDisposable which would allow you to close the transaction
    // without closing the connection. However, we don't currently have any use-cases for that
    public async ValueTask BeginTransactionAsync()
    {
        Invariant.Require(!this.HasTransaction);

        using var _ = await this.ConnectionMonitor.AcquireConnectionLockAsync(CancellationToken.None).ConfigureAwait(false);

        this._transaction =
#if NETSTANDARD2_1
         !SyncViaAsync.IsSynchronous && this.InnerConnection is DbConnection dbConnection
            ? await dbConnection.BeginTransactionAsync().ConfigureAwait(false)
            : 
#elif NETSTANDARD2_0 || NET461
#else
        ERROR
#endif
            this.InnerConnection.BeginTransaction();
    }

    public async ValueTask OpenAsync(CancellationToken cancellationToken)
    {
        if ((cancellationToken.CanBeCanceled || !SyncViaAsync.IsSynchronous)
            && this.InnerConnection is DbConnection dbConnection)
        {
            await dbConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.InnerConnection.Open();
        }

        this.ConnectionMonitor.Start();
    }

    public ValueTask CloseAsync() => this.DisposeOrCloseAsync(isDispose: false);
    public ValueTask DisposeAsync() => this.DisposeOrCloseAsync(isDispose: true);

    private async ValueTask DisposeOrCloseAsync(bool isDispose)
    {
        Invariant.Require(isDispose || !this.IsExernallyOwned);

        try 
        { 
            await (isDispose ? this.ConnectionMonitor.DisposeAsync() : this.ConnectionMonitor.StopAsync()).ConfigureAwait(false); 
        }
        finally
        {
            if (!this.IsExernallyOwned)
            {
                try { await this.DisposeTransactionAsync(isClosingOrDisposingConnection: true).ConfigureAwait(false); }
                finally
                {
#if NETSTANDARD2_1
                    if (!SyncViaAsync.IsSynchronous && this.InnerConnection is DbConnection dbConnection)
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
        }

        void SyncDisposeConnection()
        {
            if (isDispose) { this.InnerConnection.Dispose(); }
            else { this.InnerConnection.Close(); }
        }
    }

    public ValueTask DisposeTransactionAsync() => this.DisposeTransactionAsync(isClosingOrDisposingConnection: false);

    private async ValueTask DisposeTransactionAsync(bool isClosingOrDisposingConnection)
    {
        var transaction = this._transaction;
        if (transaction == null) { return; }
        this._transaction = null;

        // we don't need the connection lock here if we're closing/disposing, since in that case we stop the monitor first
        using var _ = isClosingOrDisposingConnection 
            ? null 
            : await this.ConnectionMonitor.AcquireConnectionLockAsync(CancellationToken.None).ConfigureAwait(false);

#if NETSTANDARD2_1
        if (!SyncViaAsync.IsSynchronous && transaction is DbTransaction dbTransaction)
        {
            await dbTransaction.DisposeAsync().ConfigureAwait(false);
            return;
        }
#elif NETSTANDARD2_0 || NET461
#else
        ERROR
#endif

        transaction.Dispose();
    }

    public abstract bool IsCommandCancellationException(Exception exception);

    public abstract Task SleepAsync(TimeSpan sleepTime, CancellationToken cancellationToken, Func<DatabaseCommand, CancellationToken, ValueTask<int>> executor);
}
