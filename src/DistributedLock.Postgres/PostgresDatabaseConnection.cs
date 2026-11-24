using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using Npgsql;
using System.Data;
using System.Diagnostics;

namespace Medallion.Threading.Postgres;

internal sealed class PostgresDatabaseConnection : DatabaseConnection
{
    internal static bool PassiveSleepSupported =>
#if NET8_0_OR_GREATER
        // For now, use a switch to support disabling the new behavior if it causes issues
        AppContext.TryGetSwitch("DistributedLock.Postgres.PassiveSleep", out var enabled) ? enabled : true;
#else
            // In .NET framework canceling WaitAsync() doesn't seem to work consistently; I see this when running
            // multiple instances of the TestAccessingHandleLostTokenWhileKeepaliveActiveDoesNotBlock() test in the same
            // run. Therefore, we don't support this on older .NET versions for now
            false;
#endif

    public PostgresDatabaseConnection(IDbConnection connection)
        : base(connection, isExternallyOwned: true)
    {
    }

    public PostgresDatabaseConnection(IDbTransaction transaction)
        : base(transaction, isExternallyOwned: true)
    {
    }

#if NET7_0_OR_GREATER
    public PostgresDatabaseConnection(System.Data.Common.DbDataSource dbDataSource)
        : base(dbDataSource)
    {
    }
#endif

    public PostgresDatabaseConnection(string connectionString)
        : base(new NpgsqlConnection(connectionString), isExternallyOwned: false)
    {
    }

    // see https://www.npgsql.org/doc/prepare.html
    public override bool ShouldPrepareCommands => true;

    public override bool IsCommandCancellationException(Exception exception) =>
        exception is PostgresException postgresException
            // cancellation error code from https://www.postgresql.org/docs/10/errcodes-appendix.html
            && postgresException.SqlState == "57014";

    public override async Task SleepAsync(TimeSpan sleepTime, SleepContext context)
    {
        Invariant.Require(sleepTime >= TimeSpan.Zero);

        // Where supported, "sleep" by passively waiting for connection activity/failure without executing a
        // query. This detects connection loss as soon as the socket breaks rather than when the sleep query
        // errors, and leaves the session idle server-side. See https://github.com/madelson/DistributedLock/pull/285
        if (await this.TryPassiveSleepAsync(sleepTime, context).ConfigureAwait(false))
        {
            // the passive wait left the session idle; run a keepalive query to prevent idle session reaping
            using var keepaliveCommand = this.CreateCommand();
            keepaliveCommand.SetCommandText("SELECT 0 /* DistributedLock connection keepalive */");
            await context.ExecuteAsync(keepaliveCommand).ConfigureAwait(false);
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
            await context.ExecuteAsync(setSavePointCommand, CancellationToken.None).ConfigureAwait(false);
        }

        try
        {
            using var sleepCommand = this.CreateCommand();
            sleepCommand.SetCommandText("SELECT pg_catalog.pg_sleep(@sleepTimeSeconds)");
            sleepCommand.AddParameter("sleepTimeSeconds", sleepTime.TotalSeconds, DbType.Double);
            sleepCommand.SetTimeout(sleepTime);
            await context.ExecuteAsync(sleepCommand).ConfigureAwait(false);
        }
        finally
        {
            if (hasTransaction)
            {
                using var rollBackSavePointCommand = this.CreateCommand();
                rollBackSavePointCommand.SetCommandText("ROLLBACK TO SAVEPOINT " + SavePointName);
                await context.ExecuteAsync(rollBackSavePointCommand, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    public async ValueTask<bool> TryPassiveSleepAsync(TimeSpan timeout, SleepContext context)
    {
        if (!PassiveSleepSupported
            || context.DangerousGetInnerConnection() is not NpgsqlConnection connection
            // Passive waiting is unsupported when: Npgsql multiplexing (Wait is unsupported) or Npgsql KeepAlive
            // (cancellation mid-keepalive-exchange breaks the connection)
            || new NpgsqlConnectionStringBuilder(connection.ConnectionString) is not { Multiplexing: false, KeepAlive: 0 })
        { 
            return false;
        }

        // WaitAsync completes when ANY message arrives (e.g. a notification), not just on timeout,
        // so loop until the full timeout has elapsed
        var startTimestamp = Stopwatch.GetTimestamp();
        var remaining = timeout;
        while (await connection.WaitAsync(remaining, context.CancellationToken).ConfigureAwait(false))
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
