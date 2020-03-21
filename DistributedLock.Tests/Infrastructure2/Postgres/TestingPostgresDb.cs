using Medallion.Threading.Internal;
using Medallion.Threading.Tests.Data;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Medallion.Threading.Tests.Postgres
{
    public sealed class TestingPostgresDb : ITestingPrimaryClientDb
    {
        internal static readonly string ConnectionString = PostgresCredentials.GetConnectionString(TestContext.CurrentContext.TestDirectory);

        public DbConnectionStringBuilder ConnectionStringBuilder { get; } = new NpgsqlConnectionStringBuilder(ConnectionString);

        // https://til.hashrocket.com/posts/8f87c65a0a-postgresqls-max-identifier-length-is-63-bytes
        public int MaxApplicationNameLength => 63;

        /// <summary>
        /// Technically Postgres does support this through xact advisory lock methods, but it is very unwieldy to use due to the transaction
        /// abort semantics and largely unnecessary for our purposes since, unlike SQLServer, a connection-scoped Postgres lock can still
        /// participate in an ongoing transaction.
        /// </summary>
        public bool SupportsTransactionScopedSynchronization => false;

        public void ClearPool(DbConnection connection) => NpgsqlConnection.ClearPool((NpgsqlConnection)connection);

        public int CountActiveSessions(string applicationName)
        {
            Invariant.Require(applicationName.Length <= this.MaxApplicationNameLength);

            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*)::int FROM pg_stat_activity WHERE application_name = @applicationName";
            command.Parameters.AddWithValue("applicationName", applicationName);
            return (int)command.ExecuteScalar();
        }

        public DbConnection CreateConnection() => new NpgsqlConnection(this.ConnectionStringBuilder.ConnectionString);
    }
}
