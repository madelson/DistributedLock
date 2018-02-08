using Medallion.Shell;
using Medallion.Threading.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    [TestClass]
    public class SqlDistributedLockTest : TestBase
    {
        [TestMethod]
        public void TestBadConstructorArguments()
        {
            TestHelper.AssertThrows<ArgumentNullException>(() => new SqlDistributedLock(null, ConnectionStringProvider.ConnectionString));
            TestHelper.AssertThrows<ArgumentNullException>(() => new SqlDistributedLock("a", default(string)));
            TestHelper.AssertThrows<ArgumentNullException>(() => new SqlDistributedLock("a", default(DbTransaction)));
            TestHelper.AssertThrows<ArgumentNullException>(() => new SqlDistributedLock("a", default(DbConnection)));
            TestHelper.AssertThrows<ArgumentException>(() => new SqlDistributedLock("a", ConnectionStringProvider.ConnectionString, (SqlDistributedLockConnectionStrategy)(-1)));
            TestHelper.AssertThrows<ArgumentException>(() => new SqlDistributedLock("a", ConnectionStringProvider.ConnectionString, (SqlDistributedLockConnectionStrategy)5));
            TestHelper.AssertThrows<FormatException>(() => new SqlDistributedLock(new string('a', SqlDistributedLock.MaxLockNameLength + 1), ConnectionStringProvider.ConnectionString));
            TestHelper.AssertDoesNotThrow(() => new SqlDistributedLock(new string('a', SqlDistributedLock.MaxLockNameLength), ConnectionStringProvider.ConnectionString));
        }

        [TestMethod]
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
