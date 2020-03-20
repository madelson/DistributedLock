using Medallion.Threading.Internal;
using Medallion.Threading.Tests.Data;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Medallion.Threading.Tests.SqlServer
{
    public interface ITestingSqlServerDb : ITestingDb { }

    public sealed class TestingSqlServerDb : ITestingSqlServerDb, ITestingPrimaryClientDb
    {
        internal static readonly string ConnectionString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
            {
                DataSource = @".\SQLEXPRESS",
                InitialCatalog = "master",
                IntegratedSecurity = true,
                ApplicationName = $"{typeof(TestingSqlServerDb).Assembly.GetName().Name} ({TestHelper.FrameworkName})",
                // set a high pool size so that we don't empty the pool through things like lock abandonment tests
                MaxPoolSize = 10000,
            }
            .ConnectionString;

        public DbConnectionStringBuilder ConnectionStringBuilder { get; } = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(ConnectionString);

        // https://stackoverflow.com/questions/5808332/sql-server-maximum-character-length-of-object-names/41502228
        public int MaxApplicationNameLength => 128;

        public void ClearPool(DbConnection connection) => Microsoft.Data.SqlClient.SqlConnection.ClearPool((Microsoft.Data.SqlClient.SqlConnection)connection);

        public int CountActiveSessions(string applicationName)
        {
            Invariant.Require(applicationName.Length <= this.MaxApplicationNameLength);

            using var connection = new Microsoft.Data.SqlClient.SqlConnection(ConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = $@"SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE program_name = @applicationName";
            command.Parameters.AddWithValue("applicationName", applicationName);
            return (int)command.ExecuteScalar();
        }

        public DbConnection CreateConnection() => new Microsoft.Data.SqlClient.SqlConnection(this.ConnectionStringBuilder.ConnectionString);
    }

    public sealed class TestingSystemDataSqlServerDb : ITestingSqlServerDb
    {
        public DbConnectionStringBuilder ConnectionStringBuilder { get; } = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(TestingSqlServerDb.ConnectionString);

        public int MaxApplicationNameLength => new TestingSqlServerDb().MaxApplicationNameLength;

        public void ClearPool(DbConnection connection) => System.Data.SqlClient.SqlConnection.ClearPool((System.Data.SqlClient.SqlConnection)connection);

        public int CountActiveSessions(string applicationName) => new TestingSqlServerDb().CountActiveSessions(applicationName);

        public DbConnection CreateConnection() => new System.Data.SqlClient.SqlConnection(this.ConnectionStringBuilder.ConnectionString);
    }
}
