using Medallion.Threading.Postgres;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Postgres
{
    public class PostgresDistributedLockTest
    {
        [Test]
        public void TestValidatesConstructorArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new PostgresDistributedLock(new(0), default(string)!));
            Assert.Throws<ArgumentNullException>(() => new PostgresDistributedLock(new(0), default(IDbConnection)!));
        }

        [Test]
        public async Task TestInt64AndInt32PairKeyNamespacesAreDifferent()
        {
            var connectionString = TestingPostgresDb.ConnectionString;
            var key1 = new PostgresAdvisoryLockKey(0);
            var key2 = new PostgresAdvisoryLockKey(0, 0);
            var @lock1 = new PostgresDistributedLock(key1, connectionString);
            var @lock2 = new PostgresDistributedLock(key2, connectionString);

            using var handle1 = await lock1.TryAcquireAsync();
            Assert.IsNotNull(handle1);

            using var handle2 = await lock2.TryAcquireAsync();
            Assert.IsNotNull(handle2);
        }

        [Test]
        public async Task TestWorksWithAmbientTransaction()
        {
            using var connection = new NpgsqlConnection(TestingPostgresDb.ConnectionString);
            await connection.OpenAsync();

            var connectionLock = new PostgresDistributedLock(new PostgresAdvisoryLockKey("AmbTrans"), connection);
            var otherLock = new PostgresDistributedLock(connectionLock.Key, TestingPostgresDb.ConnectionString);
            using var otherLockHandle = await otherLock.AcquireAsync();

            using (var transaction = connection.BeginTransaction())
            {
                using var transactionCommand = connection.CreateCommand();
                transactionCommand.Transaction = transaction;

                transactionCommand.CommandText = "SET LOCAL statement_timeout = 1010";
                await transactionCommand.ExecuteNonQueryAsync();

                using (var timedOutHandle = await connectionLock.TryAcquireAsync(TimeSpan.FromSeconds(.2)))
                {
                    Assert.IsNull(timedOutHandle);
                }

                (await GetTimeoutAsync(transactionCommand)).ShouldEqual("1010ms");

                var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(.3));
                var task = connectionLock.AcquireAsync(cancellationToken: cancellationTokenSource.Token).AsTask();
                task.ContinueWith(_ => { }).Wait(TimeSpan.FromSeconds(5)).ShouldEqual(true);
                task.Status.ShouldEqual(TaskStatus.Canceled);

                (await GetTimeoutAsync(transactionCommand)).ShouldEqual("1010ms");
            }

            using var connectionCommand = connection.CreateCommand();
            (await GetTimeoutAsync(connectionCommand)).ShouldEqual("0");

            static Task<object> GetTimeoutAsync(NpgsqlCommand command)
            {
                command.CommandText = "SHOW statement_timeout";
                return command.ExecuteScalarAsync();
            }
        }
    }
}
