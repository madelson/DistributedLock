using Medallion.Threading.Internal;
using Medallion.Threading.SqlServer;
using Medallion.Threading.Tests.SqlServer;
using NUnit.Framework;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.SqlServer
{
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
                Assert.Catch<OperationCanceledException>(() => SyncViaAsync.Run(_ => command.ExecuteNonQueryAsync(cancellationTokenSource.Token), 0));
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
                else { SyncViaAsync.Run(_ => command.ExecuteNonQueryAsync(cancellationTokenSource.Token), 0); }
            });
            Assert.IsFalse(task.Wait(TimeSpan.FromSeconds(.1)));

            cancellationTokenSource.Cancel();
            Assert.IsTrue(task.ContinueWith(_ => { }).Wait(TimeSpan.FromSeconds(5)));
            task.Status.ShouldEqual(TaskStatus.Canceled);
        }

        private static SqlDatabaseConnection CreateConnection(bool isSystemDataSqlClient) =>
            new SqlDatabaseConnection(
                isSystemDataSqlClient
                    ? new System.Data.SqlClient.SqlConnection(TestingSqlServerDb.DefaultConnectionString).As<DbConnection>()
                    : new Microsoft.Data.SqlClient.SqlConnection(TestingSqlServerDb.DefaultConnectionString),
                isExternallyOwned: false
            );
    }
}
