using Medallion.Threading.Oracle;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Medallion.Threading.Tests.Oracle;

public class OracleDistributedLockTest
{
    [Test]
    public void TestValidatesConstructorArguments()
    {
        Assert.Catch<ArgumentNullException>(() => new OracleDistributedLock(null!, TestingOracleDb.DefaultConnectionString));
        Assert.Catch<ArgumentNullException>(() => new OracleDistributedLock(null!, TestingOracleDb.DefaultConnectionString, exactName: true));
        Assert.Catch<ArgumentNullException>(() => new OracleDistributedLock("a", default(string)!));
        Assert.Catch<ArgumentNullException>(() => new OracleDistributedLock("a", default(IDbConnection)!));
        Assert.Catch<FormatException>(() => new OracleDistributedLock(new string('a', OracleDistributedLock.MaxNameLength + 1), TestingOracleDb.DefaultConnectionString, exactName: true));
        Assert.DoesNotThrow(() => new OracleDistributedLock(new string('a', OracleDistributedLock.MaxNameLength), TestingOracleDb.DefaultConnectionString, exactName: true));
    }

    [Test]
    public void TestGetSafeLockNameCompat()
    {
        GetSafeName(string.Empty).ShouldEqual("EMPTYz4PhNX7vuL3xVChQ1m2AB9Yg5AULVxXcg/SpIdNs6c5H0NE8XYXysP+DGNKHfuwvY7kxvUdBeoGlODJ6+SfaPg==");
        GetSafeName("abc").ShouldEqual("abc");
        GetSafeName("ABC").ShouldEqual("ABC");
        GetSafeName("\\").ShouldEqual("\\");
        GetSafeName(new string('a', OracleDistributedLock.MaxNameLength)).ShouldEqual(new string('a', OracleDistributedLock.MaxNameLength));
        GetSafeName(new string('\\', OracleDistributedLock.MaxNameLength)).ShouldEqual(new string('\\', OracleDistributedLock.MaxNameLength));
        GetSafeName(new string('x', OracleDistributedLock.MaxNameLength + 1)).ShouldEqual("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxGQFUg+qZ+nRyj9exOtumtynPpKt8OIVz76JkHSrwV38k3VGsuu7EGnoR0Q9sTmijuQ57I0jGeEhqQ2XJ2RAc3Q==");

        static string GetSafeName(string name) => new OracleDistributedLock(name, TestingOracleDb.DefaultConnectionString).Name;
    }
}
