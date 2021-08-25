using Medallion.Threading.Internal.Data;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.MySql
{
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

        public override async Task SleepAsync(TimeSpan sleepTime, CancellationToken cancellationToken, Func<DatabaseCommand, CancellationToken, ValueTask<int>> executor)
        {
            using var sleepCommand = this.CreateCommand();
            sleepCommand.SetCommandText("SELECT SLEEP(@durationSeconds)");
            sleepCommand.AddParameter("durationSeconds", sleepTime.TotalSeconds);
            sleepCommand.SetTimeout(sleepTime);

            await executor(sleepCommand, cancellationToken).ConfigureAwait(false);
        }
    }
}
