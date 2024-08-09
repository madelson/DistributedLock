using Medallion.Threading.Postgres;
using Npgsql;
using NUnit.Framework;
using System.Data;
#if NET7_0_OR_GREATER
using System.Data.Common;
#endif

namespace Medallion.Threading.Tests.Postgres;

public class PostgresDistributedLockTest
{
    [Test]
    public void TestValidatesConstructorArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new PostgresDistributedLock(new(0), default(string)!));
        Assert.Throws<ArgumentNullException>(() => new PostgresDistributedLock(new(0), default(IDbConnection)!));
#if NET7_0_OR_GREATER
        Assert.Throws<ArgumentNullException>(() => new PostgresDistributedLock(new(0), default(DbDataSource)!));
#endif
    }

#if NET7_0_OR_GREATER
    [Test]
    public void TestMultiplexingWithDbDataSourceThrowNotSupportedException()
    {
        using var dataSource = new NpgsqlDataSourceBuilder(PostgresSetUpFixture.PostgreSql.GetConnectionString()).Build();
        Assert.Throws<NotSupportedException>(() => new PostgresDistributedLock(new(0), dataSource, opt => opt.UseMultiplexing()));
    }

    // DbDataSource just calls through to the IDbConnection flow so we don't need exhaustive testing, but we want to
    // see it at least work once
    [Test]
    public async Task TestDbDataSourceConstructorWorks()
    {
        using var dataSource = new NpgsqlDataSourceBuilder(PostgresSetUpFixture.PostgreSql.GetConnectionString()).Build();
        PostgresDistributedLock @lock = new(new(5, 5), dataSource);
        await using (await @lock.AcquireAsync())
        {
            await using var handle = await @lock.TryAcquireAsync();
            Assert.IsNull(handle);
        }
    }
#endif

    [Test]
    public async Task TestInt64AndInt32PairKeyNamespacesAreDifferent()
    {
        var connectionString = PostgresSetUpFixture.PostgreSql.GetConnectionString();
        var key1 = new PostgresAdvisoryLockKey(0);
        var key2 = new PostgresAdvisoryLockKey(0, 0);
        var @lock1 = new PostgresDistributedLock(key1, connectionString);
        var @lock2 = new PostgresDistributedLock(key2, connectionString);

        using var handle1 = await lock1.TryAcquireAsync();
        Assert.That(handle1, Is.Not.Null);

        using var handle2 = await lock2.TryAcquireAsync();
        Assert.That(handle2, Is.Not.Null);
    }

    [Test]
    public async Task TestWorksWithAmbientTransaction()
    {
        using var connection = new NpgsqlConnection(PostgresSetUpFixture.PostgreSql.GetConnectionString());
        await connection.OpenAsync();

        var connectionLock = new PostgresDistributedLock(new PostgresAdvisoryLockKey("AmbTrans"), connection);
        var otherLock = new PostgresDistributedLock(connectionLock.Key, PostgresSetUpFixture.PostgreSql.GetConnectionString());
        using var otherLockHandle = await otherLock.AcquireAsync();

        using (var transaction = connection.BeginTransaction())
        {
            using var transactionCommand = connection.CreateCommand();
            transactionCommand.Transaction = transaction;

            transactionCommand.CommandText = "SET LOCAL statement_timeout = 1010";
            await transactionCommand.ExecuteNonQueryAsync();

            using (var timedOutHandle = await connectionLock.TryAcquireAsync(TimeSpan.FromSeconds(.2)))
            {
                Assert.That(timedOutHandle, Is.Null);
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
            return command.ExecuteScalarAsync()!;
        }
    }
}