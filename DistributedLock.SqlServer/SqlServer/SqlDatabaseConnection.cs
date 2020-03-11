using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;

namespace Medallion.Threading.SqlServer
{
    internal sealed class SqlDatabaseConnection : DatabaseConnection
    {
        public SqlDatabaseConnection(IDbConnection connection, TimeoutValue keepaliveCadence)
            : base(connection, keepaliveCadence)
        {
        }

        public SqlDatabaseConnection(IDbTransaction transaction, TimeoutValue keepaliveCadence)
            : base(transaction, keepaliveCadence)
        {
        }

        public SqlDatabaseConnection(string connectionString, TimeoutValue keepaliveCadence)
            : base(new SqlConnection(connectionString), keepaliveCadence)
        {
        }

        protected override bool IsCommandCancellationException(Exception exception)
        {
            const int CanceledNumber = 0;

            // fast path using default SqlClient
            if (exception is SqlException sqlException && sqlException.Number == CanceledNumber)
            {
                return true;
            }

            var exceptionType = exception.GetType();
            // since SqlException is sealed (as of 2020-01-26)
            if (exceptionType.ToString() == "System.Data.SqlClient.SqlException")
            {
                var numberProperty = exceptionType
                    .GetProperty(nameof(SqlException.Number), BindingFlags.Public | BindingFlags.Instance);
                Invariant.Require(numberProperty != null);
                if (numberProperty != null)
                {
                    return Equals(numberProperty.GetValue(exception), CanceledNumber);
                }
            }

            // this shows up when you call DbCommand.Cancel()
            return exception is InvalidOperationException;
        }
    }
}
