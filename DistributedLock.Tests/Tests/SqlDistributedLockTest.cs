using Medallion.Shell;
using Medallion.Threading.Sql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
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
            Assert.Catch<ArgumentNullException>(() => new SqlDistributedLock("a", default(string)!));
            Assert.Catch<ArgumentNullException>(() => new SqlDistributedLock("a", default(DbTransaction)!));
            Assert.Catch<ArgumentNullException>(() => new SqlDistributedLock("a", default(DbConnection)!));
            Assert.Catch<ArgumentException>(() => new SqlDistributedLock("a", ConnectionStringProvider.ConnectionString, (SqlDistributedLockConnectionStrategy)(-1)));
            Assert.Catch<ArgumentException>(() => new SqlDistributedLock("a", ConnectionStringProvider.ConnectionString, (SqlDistributedLockConnectionStrategy)5));
            Assert.Catch<FormatException>(() => new SqlDistributedLock(new string('a', SqlDistributedLock.MaxLockNameLength + 1), ConnectionStringProvider.ConnectionString));
            Assert.DoesNotThrow(() => new SqlDistributedLock(new string('a', SqlDistributedLock.MaxLockNameLength), ConnectionStringProvider.ConnectionString));
        }

        [Test]
        public void TestGetSafeLockNameCompat()
        {
            SqlDistributedLock.GetSafeLockName("").ShouldEqual("");
            SqlDistributedLock.GetSafeLockName("abc").ShouldEqual("abc");
            SqlDistributedLock.GetSafeLockName("\\").ShouldEqual("\\");
            SqlDistributedLock.GetSafeLockName(new string('a', SqlDistributedLock.MaxLockNameLength)).ShouldEqual(new string('a', SqlDistributedLock.MaxLockNameLength));
            SqlDistributedLock.GetSafeLockName(new string('\\', SqlDistributedLock.MaxLockNameLength)).ShouldEqual(@"\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\");
            SqlDistributedLock.GetSafeLockName(new string('x', SqlDistributedLock.MaxLockNameLength + 1)).ShouldEqual("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxA3SOHbN+Zq/qt/fpO9dxauQ3kVj8wfeEbknAYembWJG1Xuf4CL0Dmx3u+dAWHzkFMdjQhlRnlAXtiH7ZMFjjsg==");
        }
    }
}
