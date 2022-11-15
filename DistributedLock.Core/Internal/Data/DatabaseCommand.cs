using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Internal.Data;

/// <summary>
/// Abstraction over <see cref="IDbCommand"/> for a <see cref="DatabaseConnection"/>
/// </summary>
#if DEBUG
public
#else
internal
#endif
sealed class DatabaseCommand : IDisposable
{
    private readonly IDbCommand _command;
    private readonly DatabaseConnection _connection;

    internal DatabaseCommand(IDbCommand command, DatabaseConnection connection)
    {
        this._command = command;
        this._connection = connection;
    }

    public IDataParameterCollection Parameters => this._command.Parameters;

    public void SetCommandText(string sql) => this._command.CommandText = sql;

    public void SetTimeout(TimeoutValue operationTimeout)
    {
        this._command.CommandTimeout = operationTimeout.IsInfinite
            // use the infinite timeout of 0
            // (see https://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqlcommand.commandtimeout%28v=vs.110%29.aspx)
            ? 0
            // command timeout is in seconds. We always wait at least the given timeout plus a buffer 
            : operationTimeout.InSeconds + 30;
    }

    public void SetCommandType(CommandType type)
    {
        this._command.CommandType = type;
    }

    public IDbDataParameter AddParameter(string? name = null, object? value = null, DbType? type = null, ParameterDirection? direction = null)
    {
        var parameter = this._command.CreateParameter();
        if (name != null) { parameter.ParameterName = name; }
        if (value != null) { parameter.Value = value; }
        if (type != null) { parameter.DbType = type.Value; }
        if (direction != null) { parameter.Direction = direction.Value; }
        this._command.Parameters.Add(parameter);
        return parameter;
    }

    #region ---- Execution ----
    public ValueTask<int> ExecuteNonQueryAsync(CancellationToken cancellationToken, bool disallowAsyncCancellation = false) =>
        this.ExecuteNonQueryAsync(cancellationToken, disallowAsyncCancellation, isConnectionMonitoringQuery: false);

    /// <summary>
    /// Internal API for <see cref="ConnectionMonitor"/>
    /// </summary>
    internal ValueTask<int> ExecuteNonQueryAsync(CancellationToken cancellationToken, bool disallowAsyncCancellation, bool isConnectionMonitoringQuery) =>
        this.ExecuteAsync((c, t) => c.ExecuteNonQueryAsync(t), c => c.ExecuteNonQuery(), cancellationToken, disallowAsyncCancellation, isConnectionMonitoringQuery);

    public ValueTask<object> ExecuteScalarAsync(CancellationToken cancellationToken, bool disallowAsyncCancellation = false) =>
        this.ExecuteAsync((c, t) => c.ExecuteScalarAsync(t), c => c.ExecuteScalar(), cancellationToken, disallowAsyncCancellation, isConnectionMonitoringQuery: false);

    private async ValueTask<TResult> ExecuteAsync<TResult>(
        Func<DbCommand, CancellationToken, Task<TResult>> executeAsync,
        Func<IDbCommand, TResult> executeSync,
        CancellationToken cancellationToken,
        bool disallowAsyncCancellation,
        bool isConnectionMonitoringQuery)
    {
        if (!SyncViaAsync.IsSynchronous && this._command is DbCommand dbCommand)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                using var _ = await this.AcquireConnectionLockIfNeeded(isConnectionMonitoringQuery).ConfigureAwait(false);
                await this.PrepareIfNeededAsync(CancellationToken.None).ConfigureAwait(false);
                return await executeAsync(dbCommand, CancellationToken.None).ConfigureAwait(false);
            }
            else if (!disallowAsyncCancellation)
            {
                return await this.InternalExecuteAndPropagateCancellationAsync(
                    (dbCommand, executeAsync),
                    (state, cancellationToken) => state.executeAsync(state.dbCommand, cancellationToken).AsValueTask(),
                    cancellationToken,
                    isConnectionMonitoringQuery
                ).ConfigureAwait(false);
            }
            else
            {
                // FALL THROUGH

                // note: we can't call ExecuteNonQueryAsync(cancellationToken) or even ExecuteNonQueryAsync() 
                // here because of a .NET bug (see https://github.com/dotnet/SqlClient/issues/44,
                // https://stackoverflow.com/questions/48461567/canceling-query-with-while-loop-hangs-forever)
                // The workaround is to fall back to sync cancellation and sync execution in this case
            }
        }

        if (cancellationToken.CanBeCanceled)
        {
            // check this first rather than rely on a race between the the cancellation registration and the
            // command execution. Note that if SqlCommand.Cancel() is called before the command is executed, this has no effect
            cancellationToken.ThrowIfCancellationRequested();

            var commandBox = new StrongBox<IDbCommand?>(this._command);

            // having the registration offload the cancel loop to a background thread is important, since
            // registrations fire synchronously if the token is already canceled
            using var registration = cancellationToken.Register(state => Task.Run(async () =>
            {
                var commandBox = (StrongBox<IDbCommand?>)state;
                IDbCommand? command;
                while ((command = Volatile.Read(ref commandBox.Value)) != null)
                {
                    try { command.Cancel(); }
                    catch { /* just ignore errors here */ }

                    await Task.Delay(1).ConfigureAwait(false);
                }
            }), state: commandBox);

            try
            {
                return await this.InternalExecuteAndPropagateCancellationAsync(
                    (command: this._command, executeSync),
                    (state, cancellationToken) => state.executeSync(state.command).AsValueTask(),
                    cancellationToken,
                    isConnectionMonitoringQuery
                ).ConfigureAwait(false);
            }
            finally
            {
                // allows the cancellation loop to exit if it started
                Volatile.Write(ref commandBox.Value, null);
            }
        }

        using var __ = await this.AcquireConnectionLockIfNeeded(isConnectionMonitoringQuery).ConfigureAwait(false);
        return executeSync(this._command);
    }

    private async ValueTask<TResult> InternalExecuteAndPropagateCancellationAsync<TState, TResult>(
        TState state,
        Func<TState, CancellationToken, ValueTask<TResult>> executeAsync,
        CancellationToken cancellationToken,
        bool isConnectionMonitoringQuery)
    {
        Invariant.Require(cancellationToken.CanBeCanceled);

        using var _ = await this.AcquireConnectionLockIfNeeded(isConnectionMonitoringQuery).ConfigureAwait(false);
        // Note: for now we cannot pass cancellationToken to PrepareAsync() because this will break on Postgres which
        // is the only db we currently support that needs Prepare currently. See https://github.com/npgsql/npgsql/issues/4209
        await this.PrepareIfNeededAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            return await executeAsync(state, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
            // Canceled SQL operations throw SqlException/InvalidOperationException instead of OCE.
            // That means that downstream operations end up faulted instead of canceled. We
            // wrap with OCE here to correctly propagate cancellation
            when (cancellationToken.IsCancellationRequested && this._connection.IsCommandCancellationException(ex))
        {
            throw new OperationCanceledException(
                "Command was canceled",
                ex,
                cancellationToken
            );
        }
    }

    private ValueTask PrepareIfNeededAsync(CancellationToken cancellationToken)
    {
        if (this._connection.ShouldPrepareCommands)
        {
#if NETSTANDARD2_1
            if (!SyncViaAsync.IsSynchronous && this._command is DbCommand dbCommand)
            {
                return dbCommand.PrepareAsync(cancellationToken).AsValueTask();
            }
#elif !NETSTANDARD2_0 && !NET461
            ERROR
#endif

            this._command.Prepare();
        }

        return default;
    }
#endregion

    public void Dispose() => this._command.Dispose();

    // NOTE: we do not accept cancellation token here since the keepalive lock should never be held for very long except in
    // bug scenarios (e. g. multi-threaded use of a connection)
    private ValueTask<IDisposable?> AcquireConnectionLockIfNeeded(bool isConnectionMonitoringQuery) =>
        isConnectionMonitoringQuery
            ? default(IDisposable?).AsValueTask()
            : this._connection.ConnectionMonitor?.AcquireConnectionLockAsync(CancellationToken.None).Convert(To<IDisposable?>.ValueTask)
                ?? default(IDisposable?).AsValueTask();
}
