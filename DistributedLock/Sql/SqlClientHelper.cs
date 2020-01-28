using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Reflection;
using System.Linq;
#if NET45 || NETSTANDARD1_3
using System.Data.SqlClient;
#elif NETSTANDARD2_0
using Microsoft.Data.SqlClient;
#endif

namespace Medallion.Threading.Sql
{
    // todo merge with SqlHelpers?
    internal static class SqlClientHelper
    {
        public static DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);

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
}
