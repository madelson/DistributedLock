using Medallion.Threading.Internal;
using Medallion.Threading.Tests.Data;
using Npgsql;
using NpgsqlTypes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Postgres;

public sealed class TestingPostgresDb : TestingPrimaryClientDb
{
    internal static readonly string DefaultConnectionString = PostgresCredentials.GetConnectionString(TestContext.CurrentContext.TestDirectory);

    private readonly NpgsqlConnectionStringBuilder _connectionStringBuilder = new NpgsqlConnectionStringBuilder(DefaultConnectionString);

    public override DbConnectionStringBuilder ConnectionStringBuilder => this._connectionStringBuilder;

    // https://til.hashrocket.com/posts/8f87c65a0a-postgresqls-max-identifier-length-is-63-bytes
    public override int MaxApplicationNameLength => 63;

    /// <summary>
    /// Technically Postgres does support this through xact advisory lock methods, but it is very unwieldy to use due to the transaction
    /// abort semantics and largely unnecessary for our purposes since, unlike SQLServer, a connection-scoped Postgres lock can still
    /// participate in an ongoing transaction.
    /// </summary>
    public override TransactionSupport TransactionSupport => TransactionSupport.ImplicitParticipation;

    public override int CountActiveSessions(string applicationName)
    {
        Invariant.Require(applicationName.Length <= this.MaxApplicationNameLength);

        using var connection = new NpgsqlConnection(DefaultConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*)::int FROM pg_stat_activity WHERE application_name = @applicationName";
        command.Parameters.AddWithValue("applicationName", applicationName);
        return (int)command.ExecuteScalar()!;
    }

    public override IsolationLevel GetIsolationLevel(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        // values based on https://www.postgresql.org/docs/12/transaction-iso.html
        command.CommandText = "SELECT REPLACE(current_setting('transaction_isolation'), ' ', '')";
        return (IsolationLevel)Enum.Parse(typeof(IsolationLevel), (string)command.ExecuteScalar(), ignoreCase: true);
    }

    public override DbConnection CreateConnection() => new NpgsqlConnection(this.ConnectionStringBuilder.ConnectionString);

    public override async Task KillSessionsAsync(string applicationName, DateTimeOffset? idleSince)
    {
        using var connection = new NpgsqlConnection(DefaultConnectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        // based on https://stackoverflow.com/questions/13236160/is-there-a-timeout-for-idle-postgresql-connections
        command.CommandText = @"
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE application_name = @applicationName
                    AND (
                        @idleSince IS NULL
                        OR (state = 'idle' AND state_change < @idleSince)
                    )";
        command.Parameters.AddWithValue("applicationName", applicationName);
        command.Parameters.Add(new NpgsqlParameter("idleSince", idleSince ?? DBNull.Value.As<object>()) { NpgsqlDbType = NpgsqlDbType.TimestampTz });

        await command.ExecuteNonQueryAsync();
    }
}
