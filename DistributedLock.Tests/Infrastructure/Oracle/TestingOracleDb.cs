using Medallion.Threading.Internal;
using Medallion.Threading.Oracle;
using Medallion.Threading.Tests.Data;
using NUnit.Framework;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Oracle
{
    public sealed class TestingOracleDb : ITestingPrimaryClientDb
    {
        internal static readonly string ConnectionString = OracleCredentials.GetConnectionString(TestContext.CurrentContext.TestDirectory);

        private readonly OracleConnectionStringBuilder _connectionStringBuilder = new OracleConnectionStringBuilder(ConnectionString);

        public DbConnectionStringBuilder ConnectionStringBuilder => this._connectionStringBuilder;

        public string ApplicationName { get; set; } = string.Empty;

        string ITestingDb.ConnectionString =>
            (this.ApplicationName.Length > 0 ? $"{OracleDatabaseConnection.ApplicationNameIndicatorPrefix}{this.ApplicationName};" : string.Empty)
                + this.ConnectionStringBuilder.ConnectionString;

        public int MaxPoolSize { get => this._connectionStringBuilder.MaxPoolSize; set => this._connectionStringBuilder.MaxPoolSize = value; }

        // see https://docs.oracle.com/database/121/ARPLS/d_appinf.htm#ARPLS65237
        public int MaxApplicationNameLength => 64;

        public TransactionSupport TransactionSupport => throw new NotImplementedException();

        public void ClearPool(DbConnection connection) => OracleConnection.ClearPool((OracleConnection)connection);

        public int CountActiveSessions(string applicationName)
        {
            Invariant.Require(applicationName.Length <= this.MaxApplicationNameLength);

            using var connection = new OracleConnection(ConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM v$session WHERE client_info = :applicationName AND status != 'KILLED'";
            command.Parameters.Add("applicationName", applicationName);
            return (int)(decimal)command.ExecuteScalar()!;
        }

        public DbConnection CreateConnection() => OracleDatabaseConnection.CreateConnection(this.As<ITestingDb>().ConnectionString);

        public IsolationLevel GetIsolationLevel(DbConnection connection)
        {
            // After briefly trying the various approaches mentioned on https://stackoverflow.com/questions/10711204/how-to-check-isoloation-level
            // I could not get them to work. Given that the tests using this are checking something relatively minor and SQLServer specific, not
            // supporting this seems fine.
            throw new NotSupportedException();
        }

        public async Task KillSessionsAsync(string applicationName, DateTimeOffset? idleSince = null)
        {
            using var connection = new OracleConnection(ConnectionString);
            await connection.OpenAsync();

            using var getIdleSessionsCommand = connection.CreateCommand();
            var idleTimeSeconds = idleSince.HasValue ? (DateTimeOffset.Now - idleSince.Value).TotalSeconds : default(double?);
            getIdleSessionsCommand.CommandText = $@"
                SELECT sid, serial# 
                FROM v$session 
                WHERE client_info = :applicationName
                    {(idleTimeSeconds.HasValue ? $"AND last_call_et >= {idleTimeSeconds}" : string.Empty)}";
            getIdleSessionsCommand.Parameters.Add("applicationName", applicationName);
            using var reader = await getIdleSessionsCommand.ExecuteReaderAsync();
            var sessionsToKill = new List<(int Sid, int SerialNumber)>();
            while (await reader.ReadAsync())
            {
                sessionsToKill.Add((Sid: (int)reader.GetDecimal(0), SerialNumber: (int)reader.GetDecimal(1)));
            }

            foreach (var (sid, serialNumber) in sessionsToKill)
            {
                using var killCommand = connection.CreateCommand();
                killCommand.CommandText = $"ALTER SYSTEM KILL SESSION '{sid},{serialNumber}'";
                await killCommand.ExecuteNonQueryAsync();
            }
        }
    }
}
