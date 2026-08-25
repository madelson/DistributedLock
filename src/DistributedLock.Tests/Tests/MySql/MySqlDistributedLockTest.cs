using Medallion.Threading.MySql;
using Medallion.Threading.Tests.Data;
using MySqlConnector;
using NUnit.Framework;
using System.Data;

namespace Medallion.Threading.Tests.MySql;

public class MySqlDistributedLockTest
{
    private static readonly string ConnectionString = new TestingMySqlDb().ConnectionStringBuilder.ConnectionString;

    [Test]
    public void TestValidatesConstructorArguments()
    {
        Assert.Catch<ArgumentNullException>(() => new MySqlDistributedLock(null!, ConnectionString));
        Assert.Catch<ArgumentNullException>(() => new MySqlDistributedLock(null!, ConnectionString, exactName: true));
        Assert.Catch<ArgumentNullException>(() => new MySqlDistributedLock("a", default(string)!));
        Assert.Catch<ArgumentNullException>(() => new MySqlDistributedLock("a", default(IDbTransaction)!));
        Assert.Catch<ArgumentNullException>(() => new MySqlDistributedLock("a", default(IDbConnection)!));
#if NET7_0_OR_GREATER
        Assert.Catch<ArgumentNullException>(() => new MySqlDistributedLock("a", default(System.Data.Common.DbDataSource)!));
#endif
        Assert.Catch<FormatException>(() => new MySqlDistributedLock(new string('a', MySqlDistributedLock.MaxNameLength + 1), ConnectionString, exactName: true));
        Assert.DoesNotThrow(() => new MySqlDistributedLock(new string('a', MySqlDistributedLock.MaxNameLength), ConnectionString, exactName: true));
    }

    [Test]
    public void TestGetSafeLockNameCompat()
    {
        GetSafeName(string.Empty).ShouldEqual("__empty__p6ad62yppho33ytkibum5wbqhqvbcsxa");
        GetSafeName("abc").ShouldEqual("abc");
        GetSafeName("ABC").ShouldEqual("abczj4qr6tvn4a3kmgq4bukhowqyfrlxsb3");
        GetSafeName("\\").ShouldEqual("\\");
        GetSafeName(new string('a', MySqlDistributedLock.MaxNameLength)).ShouldEqual(new string('a', MySqlDistributedLock.MaxNameLength));
        GetSafeName(new string('\\', MySqlDistributedLock.MaxNameLength)).ShouldEqual(@"\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\");
        GetSafeName(new string('x', MySqlDistributedLock.MaxNameLength + 1)).ShouldEqual("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxgkd2zq6c6ey6mhs45clqg7vij6ycgo43");

       static string GetSafeName(string name) => new MySqlDistributedLock(name, ConnectionString).Name;
    }

    /// <summary>
    /// This test justifies why we have constructors for MySQL locks that take in a <see cref="System.Data.IDbTransaction"/>.
    /// Otherwise, you can't have a lock use the same connection as a transaction you're working on. Compare to
    /// <see cref="Postgres.PostgresDistributedLockTest.TestWorksWithAmbientTransaction"/>
    /// </summary>
    [TestCase(typeof(TestingMySqlDb))]
    [TestCase(typeof(TestingMariaDbDb))]
    public async Task TestMySqlCommandMustExplicitlyParticipateInTransaction(Type testingDbType)
    {
        var db = (TestingDb)Activator.CreateInstance(testingDbType)!;

        using var connection = new MySqlConnection(db.ConnectionStringBuilder.ConnectionString);
        await connection.OpenAsync();

        using var createTableCommand = connection.CreateCommand();
        createTableCommand.CommandText = "CREATE TEMPORARY TABLE distributed_lock.temp (id INT)";
        await createTableCommand.ExecuteNonQueryAsync();

        using var transaction = connection.BeginTransaction();

        using var commandInTransaction = connection.CreateCommand();
        commandInTransaction.Transaction = transaction;
        commandInTransaction.CommandText = @"INSERT INTO distributed_lock.temp (id) VALUES (1), (2)";
        await commandInTransaction.ExecuteNonQueryAsync();

        using var commandOutsideTransaction = connection.CreateCommand();
        commandOutsideTransaction.CommandText = "SELECT COUNT(*) FROM distributed_lock.temp";
        var exception = Assert.ThrowsAsync<InvalidOperationException>(commandOutsideTransaction.ExecuteScalarAsync)!;
        Assert.That(exception.Message, Does.Contain("The transaction associated with this command is not the connection's active transaction"));

        commandInTransaction.CommandText = "SELECT COUNT(*) FROM distributed_lock.temp";
        (await commandInTransaction.ExecuteScalarAsync()).ShouldEqual(2);
    }

#if NET7_0_OR_GREATER
    [Test]
    public async Task TestMultiplexingWithDbDataSourceUsesASharedConnection()
    {
        var applicationName = UniqueApplicationName();
        await using var dataSource = CreateDataSource(applicationName);

        var lock1 = new MySqlDistributedLock(Guid.NewGuid().ToString(), dataSource);
        var lock2 = new MySqlDistributedLock(Guid.NewGuid().ToString(), dataSource);
        await using var handle1 = await lock1.AcquireAsync();
        await using var handle2 = await lock2.AcquireAsync();

        Assert.That(new TestingMySqlDb().CountActiveSessions(applicationName), Is.EqualTo(1), "both locks should share one multiplexed connection");
    }

    [Test]
    public async Task TestDbDataSourcePoolIsKeyedByReference()
    {
        var applicationName = UniqueApplicationName();
        await using var dataSource1 = CreateDataSource(applicationName);
        await using var dataSource2 = CreateDataSource(applicationName);

        var lock1 = new MySqlDistributedLock(Guid.NewGuid().ToString(), dataSource1);
        var lock2 = new MySqlDistributedLock(Guid.NewGuid().ToString(), dataSource2);
        await using var handle1 = await lock1.AcquireAsync();
        await using var handle2 = await lock2.AcquireAsync();

        Assert.That(new TestingMySqlDb().CountActiveSessions(applicationName), Is.EqualTo(2), "distinct DbDataSource instances with the same connection string should not share connections");
    }

    // DbDataSource uses the same multiplexing flow as connection strings so we don't need exhaustive testing, but we
    // want to see mutual exclusion work at least once
    [Test]
    public async Task TestDbDataSourceConstructorWorks()
    {
        await using var dataSource = new MySqlDataSource(ConnectionString);
        var @lock = new MySqlDistributedLock(Guid.NewGuid().ToString(), dataSource);
        await using (await @lock.AcquireAsync())
        {
            await using var handle = await @lock.TryAcquireAsync();
            Assert.That(handle, Is.Null);
        }
    }

    private static string UniqueApplicationName() => $"dbds_test_{Guid.NewGuid():N}";

    private static MySqlDataSource CreateDataSource(string applicationName)
    {
        var connectionStringBuilder = new MySqlConnectionStringBuilder(ConnectionString) { ApplicationName = applicationName };
        return new MySqlDataSource(connectionStringBuilder.ConnectionString);
    }
#endif
}
