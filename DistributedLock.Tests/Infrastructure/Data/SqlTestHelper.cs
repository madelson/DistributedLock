using System;
using System.Data.Common;

namespace Medallion.Threading.Tests.Data
{
    internal static class SqlTestHelper
    {
        public static void ClearAllPools()
        {
            Microsoft.Data.SqlClient.SqlConnection.ClearAllPools();
            System.Data.SqlClient.SqlConnection.ClearAllPools();
        }

        public static void ClearPool(DbConnection connection)
        {
            switch (connection)
            {
                case Microsoft.Data.SqlClient.SqlConnection sqlConnection:
                    Microsoft.Data.SqlClient.SqlConnection.ClearPool(sqlConnection);
                    break;
                case System.Data.SqlClient.SqlConnection sqlConnection:
                    System.Data.SqlClient.SqlConnection.ClearPool(sqlConnection);
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected {nameof(DbConnection)} {connection.GetType()}");
            }
        }

        public static DbConnection CreateAlternateProviderConnection(string connectionString)
        {
#if NET471
            return new Microsoft.Data.SqlClient.SqlConnection(connectionString);
#elif NETCOREAPP3_1
            return new System.Data.SqlClient.SqlConnection(connectionString);
#endif
        }
    }
}
