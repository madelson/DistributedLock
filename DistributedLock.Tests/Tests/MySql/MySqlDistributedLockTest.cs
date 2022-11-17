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
        createTableCommand.CommandText = "CREATE TEMPORARY TABLE world.temp (id INT)";
        await createTableCommand.ExecuteNonQueryAsync();

        using var transaction = connection.BeginTransaction();

        using var commandInTransaction = connection.CreateCommand();
        commandInTransaction.Transaction = transaction;
        commandInTransaction.CommandText = @"INSERT INTO world.temp (id) VALUES (1), (2)";
        await commandInTransaction.ExecuteNonQueryAsync();

        using var commandOutsideTransaction = connection.CreateCommand();
        commandOutsideTransaction.CommandText = "SELECT COUNT(*) FROM world.temp";
        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => commandOutsideTransaction.ExecuteScalarAsync());
        Assert.That(exception.Message, Does.Contain("The transaction associated with this command is not the connection's active transaction"));

        commandInTransaction.CommandText = "SELECT COUNT(*) FROM world.temp";
        (await commandInTransaction.ExecuteScalarAsync()).ShouldEqual(2);
    }
}
