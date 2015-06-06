using Medallion.Shell;
using Medallion.Threading.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Sql
{
    [TestClass]
    public class SqlDistributedLockTest : DistributedLockTestBase
    {
        private static readonly string ConnectionString = new SqlConnectionStringBuilder
            {
                DataSource = @".\SQLEXPRESS",
                InitialCatalog = "master",
                IntegratedSecurity = true
            }
            .ConnectionString;

        [TestMethod]
        public void TestBadConstructorArguments()
        {
            TestHelper.AssertThrows<ArgumentNullException>(() => new SqlDistributedLock(null, ConnectionString));
            TestHelper.AssertThrows<ArgumentNullException>(() => new SqlDistributedLock("a", null));
            TestHelper.AssertThrows<FormatException>(() => new SqlDistributedLock(new string('a', SqlDistributedLock.MaxLockNameLength + 1), ConnectionString));
            TestHelper.AssertDoesNotThrow(() => new SqlDistributedLock(new string('a', SqlDistributedLock.MaxLockNameLength), ConnectionString));
        }

        [TestMethod]
        public void TestGetSafeLockNameCompat()
        {
            this.GetSafeLockName("").ShouldEqual("");
            this.GetSafeLockName("abc").ShouldEqual("abc");
            this.GetSafeLockName("\\").ShouldEqual("\\");
            this.GetSafeLockName(new string('a', SqlDistributedLock.MaxLockNameLength)).ShouldEqual(new string('a', SqlDistributedLock.MaxLockNameLength));
            this.GetSafeLockName(new string('\\', SqlDistributedLock.MaxLockNameLength)).ShouldEqual(@"\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\");
            this.GetSafeLockName(new string('x', SqlDistributedLock.MaxLockNameLength + 1)).ShouldEqual("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxA3SOHbN+Zq/qt/fpO9dxauQ3kVj8wfeEbknAYembWJG1Xuf4CL0Dmx3u+dAWHzkFMdjQhlRnlAXtiH7ZMFjjsg==");
        }

        [TestMethod]
        public void TestGarbageCollection()
        {
            var @lock = new SqlDistributedLock("gc_test", ConnectionString);
            Func<WeakReference> abandonLock = () => new WeakReference(@lock.Acquire());

            var weakHandle = abandonLock();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            // this is needed because the pool reclaims the SqlConnection but doesn't close it
            SqlConnection.ClearAllPools();

            weakHandle.IsAlive.ShouldEqual(false);
            using (var handle = @lock.TryAcquire())
            {
                Assert.IsNotNull(handle);
            }
        }

        internal override IDistributedLock CreateLock(string name)
        {
            return new SqlDistributedLock(name, ConnectionString);
        }

        internal override string GetSafeLockName(string name)
        {
            return SqlDistributedLock.GetSafeLockName(name);
        }
    }
}
