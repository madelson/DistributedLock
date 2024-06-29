using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using Npgsql;
using System.Data;
#if NET7_0_OR_GREATER
using System.Data.Common;
#endif

namespace Medallion.Threading.Postgres;

internal sealed class PostgresDatabaseConnection : DatabaseConnection
{
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
        : base(dbDataSource.CreateConnection(), isExternallyOwned: false)
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