using Medallion.Threading.SqlServer;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using System.Data;

namespace Medallion.Threading.Tests.SqlServer;

public class SqlDistributedLockTest
{
    [Test]
    public void TestBadConstructorArguments()
    {
        Assert.Catch<ArgumentNullException>(() => new SqlDistributedLock(null!, TestingSqlServerDb.DefaultConnectionString));
        Assert.Catch<ArgumentNullException>(() => new SqlDistributedLock(null!, TestingSqlServerDb.DefaultConnectionString, exactName: true));
        Assert.Catch<ArgumentNullException>(() => new SqlDistributedLock("a", default(string)!));
        Assert.Catch<ArgumentNullException>(() => new SqlDistributedLock("a", default(IDbTransaction)!));
        Assert.Catch<ArgumentNullException>(() => new SqlDistributedLock("a", default(IDbConnection)!));
        Assert.Catch<FormatException>(() => new SqlDistributedLock(new string('a', SqlDistributedLock.MaxNameLength + 1), TestingSqlServerDb.DefaultConnectionString, exactName: true));
        Assert.DoesNotThrow(() => new SqlDistributedLock(new string('a', SqlDistributedLock.MaxNameLength), TestingSqlServerDb.DefaultConnectionString, exactName: true));
    }

    [Test]
    public void TestGetSafeLockNameCompat()
    {
        SqlDistributedLock.GetSafeName("").ShouldEqual("");
        SqlDistributedLock.GetSafeName("abc").ShouldEqual("abc");
        SqlDistributedLock.GetSafeName("\\").ShouldEqual("\\");
        SqlDistributedLock.GetSafeName(new string('a', SqlDistributedLock.MaxNameLength)).ShouldEqual(new string('a', SqlDistributedLock.MaxNameLength));
        SqlDistributedLock.GetSafeName(new string('\\', SqlDistributedLock.MaxNameLength)).ShouldEqual(@"\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\");
        SqlDistributedLock.GetSafeName(new string('x', SqlDistributedLock.MaxNameLength + 1)).ShouldEqual("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxA3SOHbN+Zq/qt/fpO9dxauQ3kVj8wfeEbknAYembWJG1Xuf4CL0Dmx3u+dAWHzkFMdjQhlRnlAXtiH7ZMFjjsg==");
    }

    /// <summary>
    /// This test justifies why we have constructors for SQL Server locks that take in a <see cref="System.Data.IDbTransaction"/>.
    /// Otherwise, you can't have a lock use the same connection as a transaction you're working on. Compare to
    /// <see cref="Postgres.PostgresDistributedLockTest.TestWorksWithAmbientTransaction"/>
    /// </summary>
    [Test]
    public async Task TestSqlCommandMustParticipateInTransaction()
    {
        using var connection = new SqlConnection(TestingSqlServerDb.DefaultConnectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        using var commandInTransaction = connection.CreateCommand();
        commandInTransaction.Transaction = transaction;
        commandInTransaction.CommandText = @"CREATE TABLE foo (id INT); SELECT 1";
        (await commandInTransaction.ExecuteScalarAsync()).ShouldEqual(1);

        using var commandOutsideTransaction = connection.CreateCommand();
        commandOutsideTransaction.CommandText = "SELECT 2";
        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => commandOutsideTransaction.ExecuteScalarAsync())!;
        Assert.That(exception.Message, Does.Contain("requires the command to have a transaction when the connection assigned to the command is in a pending local transaction"));

        commandInTransaction.CommandText = "SELECT COUNT(*) FROM foo";
        (await commandInTransaction.ExecuteScalarAsync()).ShouldEqual(0);
    }
}
