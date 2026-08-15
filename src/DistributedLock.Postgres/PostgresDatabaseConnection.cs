using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using Npgsql;
using System.Data;
using System.Data.Common;

namespace Medallion.Threading.Postgres;

internal sealed class PostgresDatabaseConnection : DatabaseConnection
{
    /// <summary>
    /// Set only for connections we own and can therefore run passive monitoring on
    /// (the monitor never runs background work on externally-owned connections)
    /// </summary>
    private readonly NpgsqlConnection? _ownedNpgsqlConnection;
    private bool? _supportsPassiveMonitoring;

    public PostgresDatabaseConnection(IDbConnection connection)
        : base(connection, isExternallyOwned: true)
    {
    }

    public PostgresDatabaseConnection(IDbTransaction transaction)
        : base(transaction, isExternallyOwned: true)
    {
    }

#if NET7_0_OR_GREATER
    public PostgresDatabaseConnection(DbDataSource dbDataSource)
        : this(dbDataSource.CreateConnection())
    {
    }
#endif

    public PostgresDatabaseConnection(string connectionString)
        : this(new NpgsqlConnection(connectionString))
    {
    }

    private PostgresDatabaseConnection(DbConnection ownedConnection)
        : base(ownedConnection, isExternallyOwned: false)
    {
        this._ownedNpgsqlConnection = ownedConnection as NpgsqlConnection;
    }

    // see https://www.npgsql.org/doc/prepare.html
    public override bool ShouldPrepareCommands => true;

    public override bool IsCommandCancellationException(Exception exception) =>
        exception is PostgresException postgresException
            // cancellation error code from https://www.postgresql.org/docs/10/errcodes-appendix.html
            && postgresException.SqlState == "57014";

    public override async Task MonitorAsync(
        TimeoutValue monitoringCadence,
        TimeoutValue keepaliveCadence,
        CancellationToken cancellationToken,
        Func<DatabaseCommand, CancellationToken, ValueTask<int>> executor)
    {
        if (!this.SupportsPassiveMonitoring)
        {
            await base.MonitorAsync(monitoringCadence, keepaliveCadence, cancellationToken, executor).ConfigureAwait(false);
            return;
        }

        // Passively wait for connection activity/failure without executing a query, leaving the
        // session idle server-side. Cap the wait at the keepalive cadence so the session is never
        // seen as idle for longer than that.
        var maxWaitTime = !keepaliveCadence.IsInfinite && keepaliveCadence.CompareTo(monitoringCadence) < 0
            ? keepaliveCadence
            : monitoringCadence;
        await this._ownedNpgsqlConnection!.WaitAsync(maxWaitTime.TimeSpan, cancellationToken).ConfigureAwait(false);

        // the passive wait left the session idle; run the keepalive query to prevent idle session killing
        if (!keepaliveCadence.IsInfinite && !cancellationToken.IsCancellationRequested)
        {
            await this.ExecuteKeepaliveQueryAsync().ConfigureAwait(false);
        }
    }

    // NpgsqlConnection.Wait is unsupported with Npgsql multiplexing, and unsafe to cancel when Npgsql
    // KeepAlive is enabled (cancellation mid-keepalive-exchange breaks the connection) — monitoring
    // falls back to the pg_sleep query in those cases
    private bool SupportsPassiveMonitoring =>
        this._supportsPassiveMonitoring ??=
            this._ownedNpgsqlConnection != null
            && new NpgsqlConnectionStringBuilder(this._ownedNpgsqlConnection.ConnectionString) is { Multiplexing: false, KeepAlive: 0 };

    public override async Task SleepAsync(TimeSpan sleepTime, CancellationToken cancellationToken, Func<DatabaseCommand, CancellationToken, ValueTask<int>> executor)
    {
        Invariant.Require(sleepTime >= TimeSpan.Zero);

        // if we're in a transaction, we need to establish a savepoint so that we can roll back if we
        // get canceled without the whole transaction being aborted
        const string SavePointName = "medallion_threading_postgres_database_connection_sleep";

        var hasTransaction = this.HasTransaction;
        if (hasTransaction)
        {
            using var setSavePointCommand = this.CreateCommand();
            setSavePointCommand.SetCommandText("SAVEPOINT " + SavePointName);
            await executor(setSavePointCommand, CancellationToken.None).ConfigureAwait(false);
        }

        try
        {
            using var sleepCommand = this.CreateCommand();
            sleepCommand.SetCommandText("SELECT pg_catalog.pg_sleep(@sleepTimeSeconds)");
            sleepCommand.AddParameter("sleepTimeSeconds", sleepTime.TotalSeconds, DbType.Double);
            sleepCommand.SetTimeout(sleepTime);
            await executor(sleepCommand, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (hasTransaction)
            {
                using var rollBackSavePointCommand = this.CreateCommand();
                rollBackSavePointCommand.SetCommandText("ROLLBACK TO SAVEPOINT " + SavePointName);
                await executor(rollBackSavePointCommand, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}