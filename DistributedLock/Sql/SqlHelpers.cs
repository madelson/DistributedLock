using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    internal static class SqlHelpers
    {
        public static Task<int> ExecuteNonQueryAsync(this IDbCommand command, CancellationToken cancellationToken)
        {
            var dbCommand = command as DbCommand;
            if (dbCommand != null)
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
                // MA: canceled SQL operations throw SqlException when canceled instead of OCE.
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

        public static bool IsClosedOrBroken(this IDbConnection connection) 
            => connection.State == ConnectionState.Closed || connection.State == ConnectionState.Broken;
    }
}
