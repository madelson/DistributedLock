using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Medallion.Threading.Internal;

namespace Medallion.Threading.Data
{
    // todo consider using a wrapped class everywhere that provides this + connectionortransaction abstraction

    internal static class SqlHelpers
    {
        public static async ValueTask OpenAsync(IDbConnection connection, CancellationToken cancellationToken)
        {
            if ((cancellationToken.CanBeCanceled || !SyncOverAsync.IsSynchronous)
                && connection is DbConnection dbConnection)
            {
                await dbConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                connection.Open();
            }
        }

        public static ValueTask DisposeAsync(IDbConnection connection) => InternalDisposeAsync(connection);
        public static ValueTask DisposeAsync(IDbTransaction transaction) => InternalDisposeAsync(transaction);

        private static async ValueTask InternalDisposeAsync(IDisposable resource)
        {
            if (!SyncOverAsync.IsSynchronous && resource is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                resource.Dispose();
            }
        }

        public static ValueTask CloseAsync(IDbConnection connection)
        {
#if NETSTANDARD2_1
            if (!SyncOverAsync.IsSynchronous && connection is DbConnection dbConnection)
            {
                return dbConnection.CloseAsync().AsValueTask();
            }
#endif
            connection.Close();
            return default;
        }

        public static ValueTask<int> ExecuteNonQueryAsync(this IDbCommand command, CancellationToken cancellationToken) =>
            ExecuteAsync(command, (c, t) => c.ExecuteNonQueryAsync(t), c => c.ExecuteNonQuery(), cancellationToken);

        private static ValueTask<TResult> ExecuteAsync<TResult>(
            IDbCommand command, 
            Func<DbCommand, CancellationToken, Task<TResult>> executeAsync,
            Func<IDbCommand, TResult> executeSync,
            CancellationToken cancellationToken)
        {
            if (!SyncOverAsync.IsSynchronous && command is DbCommand dbCommand)
            {
                return cancellationToken.CanBeCanceled
                    ? InternalExecuteAndPropagateCancellationAsync((dbCommand, executeAsync), (state, token) => state.executeAsync(state.dbCommand, token).AsValueTask(), cancellationToken)
                    : executeAsync(dbCommand, cancellationToken).AsValueTask();
            }

            // todo retest: do we need to support this? If we do we'll need to add a flag to follow a sync codepath in the SqlSemaphore + cancel + sqlserver case
            // note: we can't call ExecuteNonQueryAsync(cancellationToken) here because of
            // what appears to be a .NET bug (see https://github.com/dotnet/corefx/issues/26623,
            // https://stackoverflow.com/questions/48461567/canceling-query-with-while-loop-hangs-forever)
            // The workaround is to fall back to multi-threaded async querying in the case where we
            // have a live cancellation token (less efficient but at least it works)

            if (cancellationToken.CanBeCanceled)
            {
                // todo retest: do we need this complexity or can we just call cancel once?
                var completed = false;
                using (cancellationToken.Register(() =>
                {
                    // we call cancel in a loop here until the command task completes. This is because
                    // when cancellation triggers it's possible the command hasn't even run yet. Therefore
                    // we want to keep trying until we know cancellation has worked
                    var spinWait = new SpinWait();
                    while (true)
                    {
                        try { command.Cancel(); }
                        catch { /* just ignore errors here */ }

                        if (Volatile.Read(ref completed)) { break; }
                        spinWait.SpinOnce();
                    }
                }))
                {
                    try { return executeSync(command).AsValueTask(); }
                    finally { Volatile.Write(ref completed, true); }
                }
            }

            return executeSync(command).AsValueTask();
        }

        private static async ValueTask<TResult> InternalExecuteAndPropagateCancellationAsync<TState, TResult>(
            TState state,
            Func<TState, CancellationToken, ValueTask<TResult>> executeAsync,
            CancellationToken cancellationToken)
        {
            try
            {
                return await executeAsync(state, cancellationToken).ConfigureAwait(false);
            }
            catch (DbException ex)
                // Canceled SQL operations throw SqlException instead of OCE.
                // That means that downstream operations end up faulted instead of canceled. We
                // wrap with OCE here to correctly propagate cancellation
                when (cancellationToken.IsCancellationRequested && IsCancellationException(ex))
            {
                throw new OperationCanceledException(
                    "Command was canceled",
                    ex,
                    cancellationToken
                );
            }
        }

        public static bool IsClosedOrBroken(this IDbConnection connection) => connection.State == ConnectionState.Closed || connection.State == ConnectionState.Broken;

        public static IDbDataParameter CreateParameter(this IDbCommand command, string name, object? value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            return parameter;
        }

        public static int GetCommandTimeout(TimeoutValue operationTimeout)
        {
            return operationTimeout.IsInfinite
                // use the infinite timeout of 0
                // (see https://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqlcommand.commandtimeout%28v=vs.110%29.aspx)
                ? 0
                // command timeout is in seconds. We always wait at least the given timeout plus a buffer 
                : operationTimeout.InSeconds + 30;
        }

        public static bool IsCancellationException(DbException exception)
        {
            const int CanceledNumber = 0;

            // fast path using default SqlClient
            if (exception is SqlException sqlException && sqlException.Number == CanceledNumber)
            {
                return true;
            }

            const string AlternateClientSqlExceptionName =
#if NETSTANDARD1_3 || NET45
                "Microsoft.Data.SqlClient.SqlException";
#else
                "System.Data.SqlClient.SqlException";
#endif
            var exceptionType = exception.GetType();
            // since SqlException is sealed in both providers (as of 2020-01-26), 
            // we don't need to search up the type hierarchy
            if (exceptionType.ToString() == AlternateClientSqlExceptionName)
            {
                var numberProperty = exceptionType.GetTypeInfo().DeclaredProperties
                    .FirstOrDefault(p => p.Name == nameof(SqlException.Number) && p.CanRead && p.GetMethod.IsPublic && !p.GetMethod.IsStatic);
                if (numberProperty != null)
                {
                    return Equals(numberProperty.GetValue(exception), CanceledNumber);
                }
            }

            return false;
        }
    }

    internal readonly struct ConnectionOrTransaction
    {
        private readonly object connectionOrTransaction;

        public IDbTransaction? Transaction => this.connectionOrTransaction as IDbTransaction;
        public IDbConnection? Connection => this.Transaction?.Connection ?? (this.connectionOrTransaction as IDbConnection);

        public ConnectionOrTransaction(IDbConnection connection)
        {
            this.connectionOrTransaction = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public ConnectionOrTransaction(IDbTransaction transaction)
        {
            this.connectionOrTransaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        }

        public static implicit operator ConnectionOrTransaction(DbTransaction transaction) => new ConnectionOrTransaction(transaction);

        public static implicit operator ConnectionOrTransaction(DbConnection connection) => new ConnectionOrTransaction(connection);
    }
}
