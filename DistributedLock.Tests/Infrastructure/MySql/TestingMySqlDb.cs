using Medallion.Threading.Tests.Data;
using MySqlConnector;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.MySql;

public class TestingMySqlDb : TestingPrimaryClientDb
{
    private readonly string _defaultConnectionString;
    private readonly MySqlConnectionStringBuilder _connectionStringBuilder;

    public TestingMySqlDb() :
        this(MySqlCredentials.GetConnectionString(TestContext.CurrentContext.TestDirectory))
    {
    }

    protected TestingMySqlDb(string defaultConnectionString)
    {
        this._defaultConnectionString = defaultConnectionString;
        this._connectionStringBuilder = new MySqlConnectionStringBuilder(this._defaultConnectionString);
    }

    public override DbConnectionStringBuilder ConnectionStringBuilder => this._connectionStringBuilder;

    public override int MaxPoolSize { get => (int)this._connectionStringBuilder.MaximumPoolSize; set => this._connectionStringBuilder.MaximumPoolSize = (uint)value; }

    public override int MaxApplicationNameLength => 65390; // based on empirical testing

    public override TransactionSupport TransactionSupport => TransactionSupport.ExplicitParticipation;

    protected virtual string IsolationLevelVariableName => "transaction_isolation";

    public override int CountActiveSessions(string applicationName)
    {
        using var connection = new MySqlConnection(this._defaultConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM performance_schema.session_connect_attrs WHERE ATTR_NAME = 'program_name' AND ATTR_VALUE = @applicationName";
        command.Parameters.AddWithValue(nameof(applicationName), applicationName);
        return (int)(long)command.ExecuteScalar()!;
    }

    public override DbConnection CreateConnection() => new MySqlConnection(this.ConnectionStringBuilder.ConnectionString);
    public override IsolationLevel GetIsolationLevel(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT @@" + this.IsolationLevelVariableName;
        var rawIsolationLevel = (string)command.ExecuteScalar()!;
        return (IsolationLevel)Enum.Parse(typeof(IsolationLevel), rawIsolationLevel.Replace("-", string.Empty), ignoreCase: true);
    }

    public override async Task KillSessionsAsync(string applicationName, DateTimeOffset? idleSince = null)
    {
        var minTimeSeconds = idleSince.HasValue
            ? (int?)(DateTimeOffset.UtcNow - idleSince.Value).TotalSeconds
            : null;

        using var connection = new MySqlConnection(this._defaultConnectionString);
        await connection.OpenAsync();
        using var idleSessionsCommand = connection.CreateCommand();
        idleSessionsCommand.CommandText = @"
                SELECT a.PROCESSLIST_ID
                FROM performance_schema.session_connect_attrs a
                JOIN information_schema.processlist p
                    ON p.ID = a.PROCESSLIST_ID
                WHERE a.ATTR_NAME = 'program_name' 
                    AND a.ATTR_VALUE = @applicationName
                    AND (@minTimeSeconds IS NULL OR p.TIME > @minTimeSeconds)";
        idleSessionsCommand.Parameters.AddWithValue(nameof(applicationName), applicationName);
        idleSessionsCommand.Parameters.AddWithValue(nameof(minTimeSeconds), minTimeSeconds);

        var idsToKill = new List<int>();
        await using (var reader = await idleSessionsCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync()) { idsToKill.Add(reader.GetInt32(0)); }
        }

        foreach (var idToKill in idsToKill)
        {
            using var killCommand = connection.CreateCommand();
            killCommand.CommandText = $"KILL {idToKill}";
            await killCommand.ExecuteNonQueryAsync();
        }
    }
}

public sealed class TestingMariaDbDb : TestingMySqlDb
{
    public TestingMariaDbDb() : base(MariaDbCredentials.GetConnectionString(TestContext.CurrentContext.TestDirectory)) { }

    protected override string IsolationLevelVariableName => "tx_isolation";
}
