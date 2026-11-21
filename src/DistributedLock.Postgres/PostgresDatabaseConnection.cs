using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using Npgsql;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace Medallion.Threading.Postgres;

internal sealed class PostgresDatabaseConnection : DatabaseConnection
{
    /// <summary>
    /// Only safe to use inside <see cref="SleepAsync"/>, where the connection monitor holds the connection lock.
    /// Non-default only for connections we own (passive monitoring never touches externally-owned connections).
    /// </summary>
    private readonly PassiveSleepHelper _sleepHelper;

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
        // Note: we only set this for internally-owned connections because those are the only
        // ones where we monitor and do SleepAsync()
        this._sleepHelper = new(ownedConnection);
    }

    // see https://www.npgsql.org/doc/prepare.html
    public override bool ShouldPrepareCommands => true;

    public override bool IsCommandCancellationException(Exception exception) =>
        exception is PostgresException postgresException
            // cancellation error code from https://www.postgresql.org/docs/10/errcodes-appendix.html
            && postgresException.SqlState == "57014";

    public override async Task SleepAsync(TimeSpan sleepTime, CancellationToken cancellationToken, Func<DatabaseCommand, CancellationToken, ValueTask<int>> executor)
    {
        Invariant.Require(sleepTime >= TimeSpan.Zero);

        // Where supported, "sleep" by passively waiting for connection activity/failure without executing a
        // query. This detects connection loss as soon as the socket breaks rather than when the sleep query
        // errors, and leaves the session idle server-side. See https://github.com/madelson/DistributedLock/pull/285
        if (await this._sleepHelper.TrySleepAsync(sleepTime, cancellationToken).ConfigureAwait(false))
        {
            // the passive wait left the session idle; run a keepalive query to prevent idle session reaping
            using var keepaliveCommand = this.CreateCommand();
            keepaliveCommand.SetCommandText("SELECT 0 /* DistributedLock connection keepalive */");
            await executor(keepaliveCommand, cancellationToken).ConfigureAwait(false);
            return;
        }

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

    /// <summary>
    /// Exposes only <see cref="NpgsqlConnection.WaitAsync(TimeSpan, CancellationToken)"/> from the wrapped
    /// connection, keeping the rest of the (non-thread-safe) connection surface inaccessible.
    /// </summary>
    internal readonly struct PassiveSleepHelper(DbConnection dbConnection)
    {
        private readonly NpgsqlConnection? _connection = dbConnection as NpgsqlConnection;

        public static bool Supported =>
#if NET8_0_OR_GREATER
            // For now, use a switch to support disabling the new behavior if it causes issues
            AppContext.TryGetSwitch("DistributedLock.Postgres.PassiveSleep", out var enabled) ? enabled : true;
#else
            // In .NET framework canceling WaitAsync() doesn't seem to work consistently; I see this when running
            // multiple instances of the TestAccessingHandleLostTokenWhileKeepaliveActiveDoesNotBlock() test in the same
            // run. Therefore, we don't support this on older .NET versions for now
            false;
#endif

        public async ValueTask<bool> TrySleepAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (!Supported || this._connection is null) { return false; }

            // Passive waiting is unsupported when: Npgsql multiplexing (Wait is unsupported) or Npgsql KeepAlive
            // (cancellation mid-keepalive-exchange breaks the connection)
            if (new NpgsqlConnectionStringBuilder(this._connection.ConnectionString) is not { Multiplexing: false, KeepAlive: 0 })
            {
                return false;
            }

            // WaitAsync completes when ANY message arrives (e.g. a notification), not just on timeout,
            // so loop until the full timeout has elapsed
            var startTimestamp = Stopwatch.GetTimestamp();
            var remaining = timeout;
            while (await this._connection.WaitAsync(remaining, cancellationToken).ConfigureAwait(false))
            {
#if NET7_0_OR_GREATER
                var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
#else
                var elapsed = TimeSpan.FromSeconds((Stopwatch.GetTimestamp() - startTimestamp) / (double)Stopwatch.Frequency);
#endif
                remaining = timeout - elapsed;
                if (remaining <= TimeSpan.Zero) { break; }
            }

            return true;
        }
    }
}
