using Medallion.Threading.Postgres;
using NUnit.Framework;
using System.Data;
#if NET7_0_OR_GREATER
using System.Data.Common;
#endif

namespace Medallion.Threading.Tests.Postgres;

public class PostgresDistributedReaderWriterLockTest
{
    [Test]
    public void TestValidatesConstructorArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new PostgresDistributedReaderWriterLock(new(0), default(string)!));
        Assert.Throws<ArgumentNullException>(() => new PostgresDistributedReaderWriterLock(new(0), default(IDbConnection)!));
#if NET7_0_OR_GREATER
        Assert.Throws<ArgumentNullException>(() => new PostgresDistributedReaderWriterLock(new(0), default(DbDataSource)!));
#endif
    }
}