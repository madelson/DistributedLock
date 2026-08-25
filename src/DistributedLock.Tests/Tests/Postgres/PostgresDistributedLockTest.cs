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
    public async Task TestMultiplexingWithDbDataSourceUsesASharedConnection()
    {
        var applicationName = UniqueApplicationName();
        await using var dataSource = CreateDataSource(applicationName);

        var lock1 = new PostgresDistributedLock(new(Guid.NewGuid().ToString(), allowHashing: true), dataSource);
        var lock2 = new PostgresDistributedLock(new(Guid.NewGuid().ToString(), allowHashing: true), dataSource);
        await using var handle1 = await lock1.AcquireAsync();
        await using var handle2 = await lock2.AcquireAsync();

        Assert.That(await CountSessionsAsync(applicationName), Is.EqualTo(1), "both locks should share one multiplexed connection");
    }

    [Test]
    public async Task TestDbDataSourcePoolIsKeyedByReference()
    {
        var applicationName = UniqueApplicationName();
        await using var dataSource1 = CreateDataSource(applicationName);
        await using var dataSource2 = CreateDataSource(applicationName);

        var lock1 = new PostgresDistributedLock(new(Guid.NewGuid().ToString(), allowHashing: true), dataSource1);
        var lock2 = new PostgresDistributedLock(new(Guid.NewGuid().ToString(), allowHashing: true), dataSource2);
        await using var handle1 = await lock1.AcquireAsync();
        await using var handle2 = await lock2.AcquireAsync();

        Assert.That(await CountSessionsAsync(applicationName), Is.EqualTo(2), "distinct DbDataSource instances with the same connection string should not share connections");
    }

    [Test]
    public async Task TestHandleLostTokenWorksWithDbDataSourceMultiplexing()
    {
        var applicationName = UniqueApplicationName();
        await using var dataSource = CreateDataSource(applicationName);

        var @lock = new PostgresDistributedLock(new(Guid.NewGuid().ToString(), allowHashing: true), dataSource);
        var handle = await @lock.AcquireAsync();

        using var handleLostEvent = new ManualResetEventSlim(initialState: false);
        Assert.That(handle.HandleLostToken.CanBeCanceled, Is.True); // starts monitoring on the multiplexed connection
        using var registration = handle.HandleLostToken.Register(handleLostEvent.Set);

        await new TestingPostgresDb().KillSessionsAsync(applicationName, idleSince: null);

        Assert.That(handleLostEvent.Wait(TimeSpan.FromSeconds(10)), Is.True);

        // dispose may throw since the underlying connection is broken
        try { handle.Dispose(); } catch { }
    }

    // DbDataSource uses the same multiplexing flow as connection strings so we don't need exhaustive testing, but we
    // want to see mutual exclusion work at least once
    [Test]
    public async Task TestDbDataSourceConstructorWorks()
    {
        using var dataSource = new NpgsqlDataSourceBuilder(TestingPostgresDb.DefaultConnectionString).Build();
        PostgresDistributedLock @lock = new(new(5, 5), dataSource);
        await using (await @lock.AcquireAsync())
        {
            await using var handle = await @lock.TryAcquireAsync();
            Assert.IsNull(handle);
        }
    }

    private static string UniqueApplicationName() => $"dbds_test_{Guid.NewGuid():N}";

    private static NpgsqlDataSource CreateDataSource(string applicationName)
    {
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(TestingPostgresDb.DefaultConnectionString) { ApplicationName = applicationName };
        return new NpgsqlDataSourceBuilder(connectionStringBuilder.ConnectionString).Build();
    }

    private static async Task<int> CountSessionsAsync(string applicationName)
    {
        using var connection = new NpgsqlConnection(TestingPostgresDb.DefaultConnectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*)::int FROM pg_stat_activity WHERE application_name = @applicationName";
        command.Parameters.AddWithValue("applicationName", applicationName);
        return (int)(await command.ExecuteScalarAsync())!;
    }
#endif

    [Test]
    public async Task TestInt64AndInt32PairKeyNamespacesAreDifferent()
    {
        var connectionString = TestingPostgresDb.DefaultConnectionString;
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
    public async Task TestWorksWithInternalTransaction()
    {
        using var connection = new NpgsqlConnection(TestingPostgresDb.DefaultConnectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();

        var transactionLock = new PostgresDistributedLock(new PostgresAdvisoryLockKey("InternTrans", true), TestingPostgresDb.DefaultConnectionString, o => o.UseTransaction());

        using (var transactionLockHandle = await transactionLock.TryAcquireAsync(TimeSpan.FromSeconds(.3)))
        {
            (await GetTimeoutAsync("lock_timeout", command)).ShouldEqual("0");
        }

        (await GetTimeoutAsync("lock_timeout", command)).ShouldEqual("0");
    }

    [Test]
    public async Task TestWorksWithAmbientTransaction(
        [Values("1010ms", "1d", "5min", "20h", "3s")] string timeout)
    {
        using var connection = new NpgsqlConnection(TestingPostgresDb.DefaultConnectionString);
        await connection.OpenAsync();

        var connectionLock = new PostgresDistributedLock(new PostgresAdvisoryLockKey("AmbTrans"), connection);
        var otherLock = new PostgresDistributedLock(connectionLock.Key, TestingPostgresDb.DefaultConnectionString);
        using var otherLockHandle = await otherLock.AcquireAsync();

        using (var transaction = connection.BeginTransaction())
        {
            using var transactionCommand = connection.CreateCommand();
            transactionCommand.Transaction = transaction;

            transactionCommand.CommandText = $"SET LOCAL statement_timeout = '{timeout}'";
            await transactionCommand.ExecuteNonQueryAsync();

            using (var timedOutHandle = await connectionLock.TryAcquireAsync(TimeSpan.FromSeconds(.2)))
            {
                (await GetTimeoutAsync("statement_timeout", transactionCommand)).ShouldEqual(timeout);

                Assert.That(timedOutHandle, Is.Null);
            }

            (await GetTimeoutAsync("statement_timeout", transactionCommand)).ShouldEqual(timeout);

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(.3));
            var task = connectionLock.AcquireAsync(cancellationToken: cancellationTokenSource.Token).AsTask();
            task.ContinueWith(_ => { }).Wait(TimeSpan.FromSeconds(5)).ShouldEqual(true);
            task.Status.ShouldEqual(TaskStatus.Canceled);

            (await GetTimeoutAsync("statement_timeout", transactionCommand)).ShouldEqual(timeout);
        }

        using var connectionCommand = connection.CreateCommand();
        (await GetTimeoutAsync("statement_timeout", connectionCommand)).ShouldEqual("0");
    }

    private static Task<object> GetTimeoutAsync(string timeoutName, NpgsqlCommand command)
    {
        command.CommandText = $"SHOW {timeoutName}";
        return command.ExecuteScalarAsync()!;
    }
}