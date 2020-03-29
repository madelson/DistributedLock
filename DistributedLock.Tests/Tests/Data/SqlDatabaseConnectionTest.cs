using Medallion.Threading.Data;
using Medallion.Threading.Internal;
using Medallion.Threading.SqlServer;
using Medallion.Threading.Tests.SqlServer;
using NUnit.Framework;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Data
{
    // todo should this be extended to cover all DatabaseConnections? if not it should move out of Data
    public class SqlDatabaseConnectionTest
    {
        [Test, Combinatorial]
        public async Task TestExecuteNonQueryAlreadyCanceled(
            [Values] bool isAsync, 
            [Values] bool isSystemDataSqlClient,
            [Values] bool isFastQuery)
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            await using var connection = CreateConnection(isSystemDataSqlClient);
            await connection.OpenAsync(CancellationToken.None);
            using var command = connection.CreateCommand();
            command.SetCommandText(
                isFastQuery
                    ? "SELECT 1"
                    : @"WHILE 1 = 1
                        BEGIN
                            DECLARE @x INT = 1
                        END"
            );

            if (isAsync)
            {
                Assert.CatchAsync<OperationCanceledException>(() => command.ExecuteNonQueryAsync(cancellationTokenSource.Token).AsTask());
            }
            else
            {
                Assert.Catch<OperationCanceledException>(() => SyncOverAsync.Run(_ => command.ExecuteNonQueryAsync(cancellationTokenSource.Token), 0, false));
            }
        }

        [Test, Combinatorial]
        public async Task TestExecuteNonQueryCanCancel([Values] bool isAsync, [Values] bool isSystemDataSqlClient)
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            await using var connection = CreateConnection(isSystemDataSqlClient);
            await connection.OpenAsync(CancellationToken.None);
            using var command = connection.CreateCommand();
            command.SetCommandText(@"
                WHILE 1 = 1
                BEGIN
                    DECLARE @x INT = 1
                END"
            );
            
            var task = Task.Run(async () =>
            {
                if (isAsync) { await command.ExecuteNonQueryAsync(cancellationTokenSource.Token, disallowAsyncCancellation: true); }
                else { SyncOverAsync.Run(_ => command.ExecuteNonQueryAsync(cancellationTokenSource.Token), 0, false); }
            });
            Assert.IsFalse(task.Wait(TimeSpan.FromSeconds(.1)));

            cancellationTokenSource.Cancel();
            Assert.IsTrue(task.ContinueWith(_ => { }).Wait(TimeSpan.FromSeconds(5)));
            task.Status.ShouldEqual(TaskStatus.Canceled);
        }

        private static SqlDatabaseConnection CreateConnection(bool isSystemDataSqlClient) =>
            new SqlDatabaseConnection(
                isSystemDataSqlClient
                    ? new System.Data.SqlClient.SqlConnection(TestingSqlServerDb.ConnectionString).As<DbConnection>()
                    : new Microsoft.Data.SqlClient.SqlConnection(TestingSqlServerDb.ConnectionString),
                isExternallyOwned: false
            );
    }
}
