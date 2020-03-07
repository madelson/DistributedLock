using Medallion.Shell;
using Medallion.Threading.Data;
using Medallion.Threading.SqlServer;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Sql
{
    public class SqlDistributedLockTest
    {
        [Test]
        public void TestBadConstructorArguments()
        {
            Assert.Catch<ArgumentNullException>(() => new SqlDistributedLock(null!, ConnectionStringProvider.ConnectionString));
            Assert.Catch<ArgumentNullException>(() => new SqlDistributedLock(null!, ConnectionStringProvider.ConnectionString, exactName: true));
            Assert.Catch<ArgumentNullException>(() => new SqlDistributedLock("a", default(string)!));
            Assert.Catch<ArgumentNullException>(() => new SqlDistributedLock("a", default(DbTransaction)!));
            Assert.Catch<ArgumentNullException>(() => new SqlDistributedLock("a", default(DbConnection)!));
            Assert.Catch<ArgumentException>(() => new SqlDistributedLock("a", ConnectionStringProvider.ConnectionString, (SqlDistributedLockConnectionStrategy)(-1)));
            Assert.Catch<ArgumentException>(() => new SqlDistributedLock("a", ConnectionStringProvider.ConnectionString, (SqlDistributedLockConnectionStrategy)5));
            Assert.Catch<FormatException>(() => new SqlDistributedLock(new string('a', SqlDistributedLock.MaxNameLength + 1), ConnectionStringProvider.ConnectionString, exactName: true));
            Assert.DoesNotThrow(() => new SqlDistributedLock(new string('a', SqlDistributedLock.MaxNameLength), ConnectionStringProvider.ConnectionString, exactName: true));
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
    }
}
