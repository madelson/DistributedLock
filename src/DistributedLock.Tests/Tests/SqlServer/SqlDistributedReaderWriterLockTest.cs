using NUnit.Framework;
using System.Data.Common;
using Medallion.Threading.SqlServer;

namespace Medallion.Threading.Tests.SqlServer;

public sealed class SqlDistributedReaderWriterLockTest
{
    [Test]
    public void TestBadConstructorArguments()
    {
        Assert.Catch<ArgumentNullException>(() => new SqlDistributedReaderWriterLock(null!, SqlServerSetUpFixture.SqlServer.GetConnectionString()));
        Assert.Catch<ArgumentNullException>(() => new SqlDistributedReaderWriterLock(null!, SqlServerSetUpFixture.SqlServer.GetConnectionString(), exactName: true));
        Assert.Catch<ArgumentNullException>(() => new SqlDistributedReaderWriterLock("a", default(string)!));
        Assert.Catch<ArgumentNullException>(() => new SqlDistributedReaderWriterLock("a", default(DbTransaction)!));
        Assert.Catch<ArgumentNullException>(() => new SqlDistributedReaderWriterLock("a", default(DbConnection)!));
        Assert.Catch<FormatException>(() => new SqlDistributedReaderWriterLock(new string('a', SqlDistributedReaderWriterLock.MaxNameLength + 1), SqlServerSetUpFixture.SqlServer.GetConnectionString(), exactName: true));
        Assert.DoesNotThrow(() => new SqlDistributedReaderWriterLock(new string('a', SqlDistributedReaderWriterLock.MaxNameLength), SqlServerSetUpFixture.SqlServer.GetConnectionString(), exactName: true));
    }

    [Test]
    public void TestGetSafeLockNameCompat()
    {
        SqlDistributedReaderWriterLock.MaxNameLength.ShouldEqual(SqlDistributedLock.MaxNameLength);

        var cases = new[]
        {
            string.Empty,
            "abc",
            "\\",
            new string('a', SqlDistributedLock.MaxNameLength),
            new string('\\', SqlDistributedLock.MaxNameLength),
            new string('x', SqlDistributedLock.MaxNameLength + 1)
        };

        foreach (var lockName in cases)
        {
            // should be compatible with SqlDistributedLock
            SqlDistributedReaderWriterLock.GetSafeName(lockName).ShouldEqual(SqlDistributedLock.GetSafeName(lockName));
        }
    }
}
