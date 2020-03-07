using Medallion.Threading.Data;
using Medallion.Threading.Internal;
using NUnit.Framework;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Data
{
    public class SqlHelpersTest
    {
        [Test, Combinatorial]
        public async Task TestExecuteNonQueryAlreadyCanceled(
            [Values] bool isAsync, 
            [Values] bool isSystemDataSqlClient,
            [Values] bool isFastQuery)
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            using var connection = CreateConnection(isSystemDataSqlClient);
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = isFastQuery
                ? "SELECT 1"
                : @"WHILE 1 = 1
                    BEGIN
                        DECLARE @x INT = 1
                    END";

            if (isAsync)
            {
                Assert.CatchAsync<OperationCanceledException>(() => SqlHelpers.ExecuteNonQueryAsync(command, cancellationTokenSource.Token).AsTask());
            }
            else
            {
                Assert.Catch<OperationCanceledException>(() => SyncOverAsync.Run(_ => SqlHelpers.ExecuteNonQueryAsync(command, cancellationTokenSource.Token), 0, false));
            }
        }

        [Test, Combinatorial]
        public async Task TestExecuteNonQueryCanCancel([Values] bool isAsync, [Values] bool isSystemDataSqlClient)
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            using var connection = CreateConnection(isSystemDataSqlClient);
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                WHILE 1 = 1
                BEGIN
                    DECLARE @x INT = 1
                END";
            
            var task = Task.Run(async () =>
            {
                if (isAsync) { await SqlHelpers.ExecuteNonQueryAsync(command, cancellationTokenSource.Token, disallowAsyncCancellation: true); }
                else { SyncOverAsync.Run(_ => SqlHelpers.ExecuteNonQueryAsync(command, cancellationTokenSource.Token), 0, false); }
            });
            Assert.IsFalse(task.Wait(TimeSpan.FromSeconds(.1)));

            cancellationTokenSource.Cancel();
            Assert.IsTrue(task.ContinueWith(_ => { }).Wait(TimeSpan.FromSeconds(5)));
            task.Status.ShouldEqual(TaskStatus.Canceled);
        }

        private static DbConnection CreateConnection(bool isSystemDataSqlClient) =>
            isSystemDataSqlClient
                ? new System.Data.SqlClient.SqlConnection(ConnectionStringProvider.ConnectionString).As<DbConnection>()
                : new Microsoft.Data.SqlClient.SqlConnection(ConnectionStringProvider.ConnectionString);
    }
}
