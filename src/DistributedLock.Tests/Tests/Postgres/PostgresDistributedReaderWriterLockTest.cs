using Medallion.Threading.Postgres;
using NUnit.Framework;
using System.Data;

namespace Medallion.Threading.Tests.Postgres;

public class PostgresDistributedReaderWriterLockTest
{
    [Test]
    public void TestValidatesConstructorArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new PostgresDistributedReaderWriterLock(new(0), default(string)!));
        Assert.Throws<ArgumentNullException>(() => new PostgresDistributedReaderWriterLock(new(0), default(IDbConnection)!));
    }
}
