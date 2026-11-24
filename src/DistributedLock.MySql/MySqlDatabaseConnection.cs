using Medallion.Threading.Internal.Data;
using MySqlConnector;
using System.Data;

namespace Medallion.Threading.MySql;

internal class MySqlDatabaseConnection : DatabaseConnection
{
    public MySqlDatabaseConnection(IDbConnection connection)
        : base(connection, isExternallyOwned: true)
    {
    }

    public MySqlDatabaseConnection(IDbTransaction transaction)
        : base(transaction, isExternallyOwned: true)
    {
    }

#if NET7_0_OR_GREATER
    public MySqlDatabaseConnection(System.Data.Common.DbDataSource dbDataSource)
        : base(dbDataSource)
    {
    }
#endif

    public MySqlDatabaseConnection(string connectionString)
        : base(new MySqlConnection(connectionString), isExternallyOwned: false)
    {
    }

    // Seems like this only helps when executing a statement multiple times on the
    // same connection (unclear since there's limited documentation)
    public override bool ShouldPrepareCommands => false;

    public override bool IsCommandCancellationException(Exception exception)
    {
        // see https://mysqlconnector.net/overview/command-cancellation/
        return exception is MySqlException ex && ex.ErrorCode == MySqlErrorCode.QueryInterrupted;
    }

    public override async Task SleepAsync(TimeSpan sleepTime, SleepContext context)
    {
        using var sleepCommand = this.CreateCommand();
        sleepCommand.SetCommandText("SELECT SLEEP(@durationSeconds)");
        sleepCommand.AddParameter("durationSeconds", sleepTime.TotalSeconds);
        sleepCommand.SetTimeout(sleepTime);

        await context.ExecuteAsync(sleepCommand).ConfigureAwait(false);
    }
}
