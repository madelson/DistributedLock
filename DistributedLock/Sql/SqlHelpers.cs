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
        public static Task<int> ExecuteNonQueryAndPropagateCancellationAsync(this DbCommand command, CancellationToken cancellationToken)
        {
            return cancellationToken.CanBeCanceled
                ? InternalExecuteNonQueryAndPropagateCancellationAsync(command, cancellationToken)
                : command.ExecuteNonQueryAsync();
        }

        private static async Task<int> InternalExecuteNonQueryAndPropagateCancellationAsync(DbCommand command, CancellationToken cancellationToken)
        {
            try
            {
                return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                // MA: canceled SQL operations throw SqlException when canceled instead of OCE.
                // That means that downstream operations end up faulted instead of canceled. We
                // wrap with OCE here to correctly propagate cancellation
                if (cancellationToken.IsCancellationRequested && ex.Number == 0)
                {
                    throw new OperationCanceledException(
                        "Command was canceled",
                        ex,
                        cancellationToken
                    );
                }

                throw;
            }
        }

        public static bool IsClosedOrBroken(this DbConnection connection) 
            => connection.State == ConnectionState.Closed || connection.State == ConnectionState.Broken;
    }
}
