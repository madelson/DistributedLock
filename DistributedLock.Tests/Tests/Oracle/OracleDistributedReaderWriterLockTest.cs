using Medallion.Threading.Oracle;
using NUnit.Framework;
using System.Data;

namespace Medallion.Threading.Tests.Oracle;

public class OracleDistributedReaderWriterLockTest
{
    [Test]
    public void TestValidatesConstructorArguments()
    {
        Assert.Catch<ArgumentNullException>(() => new OracleDistributedReaderWriterLock(null!, TestingOracleDb.DefaultConnectionString));
        Assert.Catch<ArgumentNullException>(() => new OracleDistributedReaderWriterLock(null!, TestingOracleDb.DefaultConnectionString, exactName: true));
        Assert.Catch<ArgumentNullException>(() => new OracleDistributedReaderWriterLock("a", default(string)!));
        Assert.Catch<ArgumentNullException>(() => new OracleDistributedReaderWriterLock("a", default(IDbConnection)!));
        Assert.Catch<FormatException>(() => new OracleDistributedReaderWriterLock(new string('a', OracleDistributedLock.MaxNameLength + 1), TestingOracleDb.DefaultConnectionString, exactName: true));
        Assert.DoesNotThrow(() => new OracleDistributedReaderWriterLock(new string('a', OracleDistributedLock.MaxNameLength), TestingOracleDb.DefaultConnectionString, exactName: true));
    }

    [Test]
    public void TestGetSafeLockNameCompat()
    {
        var cases = new[]
        {
            string.Empty,
            "abc",
            "\\",
            new string('a', OracleDistributedLock.MaxNameLength),
            new string('\\', OracleDistributedLock.MaxNameLength),
            new string('x', OracleDistributedLock.MaxNameLength + 1)
        };

        foreach (var lockName in cases)
        {
            // should be compatible with OracleDistributedLock
            new OracleDistributedReaderWriterLock(lockName, TestingOracleDb.DefaultConnectionString).Name
                .ShouldEqual(new OracleDistributedLock(lockName, TestingOracleDb.DefaultConnectionString).Name);
        }
    }
}
