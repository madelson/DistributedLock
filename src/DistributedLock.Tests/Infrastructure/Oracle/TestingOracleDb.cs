using Medallion.Threading.Internal;
using Medallion.Threading.Oracle;
using Medallion.Threading.Tests.Data;
using NUnit.Framework;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;

namespace Medallion.Threading.Tests.Oracle;

public sealed class TestingOracleDb : TestingPrimaryClientDb
{
    internal static readonly string DefaultConnectionString = OracleCredentials.GetConnectionString(TestContext.CurrentContext.TestDirectory);

    private readonly OracleConnectionStringBuilder _connectionStringBuilder = new OracleConnectionStringBuilder(DefaultConnectionString);

    public override DbConnectionStringBuilder ConnectionStringBuilder => this._connectionStringBuilder;

    public override string ApplicationName { get; set; } = string.Empty;

    public override string ConnectionString =>
        (this.ApplicationName.Length > 0 ? $"{OracleDatabaseConnection.ApplicationNameIndicatorPrefix}{this.ApplicationName};" : string.Empty)
            + this.ConnectionStringBuilder.ConnectionString;

    // see https://docs.oracle.com/database/121/ARPLS/d_appinf.htm#ARPLS65237
    public override int MaxApplicationNameLength => 64;

    public override TransactionSupport TransactionSupport => TransactionSupport.ImplicitParticipation;

    public override int CountActiveSessions(string applicationName)
    {
        Invariant.Require(applicationName.Length <= this.MaxApplicationNameLength);

        using var connection = new OracleConnection(DefaultConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM v$session WHERE client_info = :applicationName AND status != 'KILLED'";
        command.Parameters.Add("applicationName", applicationName);
        return (int)(decimal)command.ExecuteScalar()!;
    }

    public override DbConnection CreateConnection() => OracleDatabaseConnection.CreateConnection(this.As<TestingDb>().ConnectionString);

    public override IsolationLevel GetIsolationLevel(DbConnection connection)
    {
        // After briefly trying the various approaches mentioned on https://stackoverflow.com/questions/10711204/how-to-check-isoloation-level
        // I could not get them to work. Given that the tests using this are checking something relatively minor and SQLServer specific, not
        // supporting this seems fine.
        throw new NotSupportedException();
    }

    public override void PrepareForHighContention(ref int maxConcurrentAcquires)
    {
        // The free Oracle Autonomous database has a fixed max session limit of 20. When concurrency approaches that, parellel
        // execution slows down greatly because often releases become queued behind competing aquires. When concurrency surpasses
        // that level we risk total deadlock where all active sessions are in use by acquires and as such no release can ever get
        // through.
        maxConcurrentAcquires = Math.Min(maxConcurrentAcquires, 15);
    }

    public override async Task KillSessionsAsync(string applicationName, DateTimeOffset? idleSince = null)
    {
        using var connection = new OracleConnection(DefaultConnectionString);
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
