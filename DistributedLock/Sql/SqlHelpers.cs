using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    internal static class SqlHelpers
    {
        public static Task<int> ExecuteNonQueryAsync(this IDbCommand command, CancellationToken cancellationToken)
        {
            if (command is DbCommand dbCommand)
            {
                return cancellationToken.CanBeCanceled
                    ? InternalExecuteNonQueryAndPropagateCancellationAsync(dbCommand, cancellationToken)
                    : dbCommand.ExecuteNonQueryAsync();
            }

            // synchronous task pattern
            var taskBuilder = new TaskCompletionSource<int>();
            if (cancellationToken.IsCancellationRequested)
            {
                taskBuilder.SetCanceled();
                return taskBuilder.Task;
            }

            try
            {
                taskBuilder.SetResult(command.ExecuteNonQuery());
            }
            catch (Exception ex)
            {
                taskBuilder.SetException(ex);
            }

            return taskBuilder.Task;
        }

        private static async Task<int> InternalExecuteNonQueryAndPropagateCancellationAsync(DbCommand command, CancellationToken cancellationToken)
        {
            try
            {
                return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (SqlException ex)
                // MA: canceled SQL operations throw SqlException instead of OCE.
                // That means that downstream operations end up faulted instead of canceled. We
                // wrap with OCE here to correctly propagate cancellation
                when (cancellationToken.IsCancellationRequested && ex.Number == 0)
            {
                throw new OperationCanceledException(
                    "Command was canceled",
                    ex,
                    cancellationToken
                );
            }
        }

        public static bool IsClosedOrBroken(this IDbConnection connection) => connection.State == ConnectionState.Closed || connection.State == ConnectionState.Broken;

        public static IDbDataParameter CreateParameter(this IDbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            return parameter;
        }

        public static int GetCommandTimeout(int operationTimeoutMillis)
        {
            return operationTimeoutMillis >= 0
              // command timeout is in seconds. We always wait at least the lock timeout plus a buffer 
              ? (operationTimeoutMillis / 1000) + 30
              // otherwise timeout is infinite so we use the infinite timeout of 0
              // (see https://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqlcommand.commandtimeout%28v=vs.110%29.aspx)
              : 0;
        }
    }

    internal struct ConnectionOrTransaction
    {
        private object connectionOrTransaction;

        public IDbTransaction Transaction => this.connectionOrTransaction as IDbTransaction;
        public IDbConnection Connection => this.Transaction?.Connection ?? (this.connectionOrTransaction as IDbConnection);

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
