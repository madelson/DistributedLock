using Medallion.Threading.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Sql
{
    [TestClass]
    public class SqlAzureDistributedLockTest : DistributedLockTestBase
    {
        [TestMethod]
        public void TestIdleSessionKiller()
        {
            using (var idleSessionKiller = new IdleSessionKiller(SqlDistributedLockTest.ConnectionString, idleTimeout: TimeSpan.FromSeconds(.25)))
            {
                var @lock = new SqlDistributedLock(nameof(TestIdleSessionKiller), SqlDistributedLockTest.ConnectionString);
                var handle = @lock.Acquire();
                Thread.Sleep(TimeSpan.FromSeconds(1));
                TestHelper.AssertThrows<SqlException>(() => handle.Dispose());
            }
        }

        [TestMethod]
        public void TestAzureStrategyProtectsFromIdleSessionKiller()
        {
            var originalInterval = KeepaliveHelper.Interval;
            try
            {
                KeepaliveHelper.Interval = TimeSpan.FromSeconds(.1);

                using (var idleSessionKiller = new IdleSessionKiller(SqlDistributedLockTest.ConnectionString, idleTimeout: TimeSpan.FromSeconds(.25)))
                {
                    var @lock = this.CreateLock(nameof(TestAzureStrategyProtectsFromIdleSessionKiller));
                    var handle = @lock.Acquire();
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    TestHelper.AssertDoesNotThrow(() => handle.Dispose());
                }
            }
            finally
            {
                KeepaliveHelper.Interval = originalInterval;
            }
        }

        /// <summary>
        /// Demonstrates that we don't multi-thread the connection despite the <see cref="KeepaliveHelper"/>
        /// </summary>
        [TestMethod]
        public void ThreadSafetyExercise()
        {
            var originalInterval = KeepaliveHelper.Interval;
            try
            {
                KeepaliveHelper.Interval = TimeSpan.FromMilliseconds(1);

                TestHelper.AssertDoesNotThrow(() =>
                {
                    var @lock = this.CreateLock(nameof(ThreadSafetyExercise));
                    for (var i = 0; i < 100; ++i)
                    {
                        using (@lock.Acquire())
                        {
                            Thread.Sleep(1);
                        }
                    }
                });
            }
            finally
            {
                KeepaliveHelper.Interval = originalInterval;
            }
        }

        internal override IDistributedLock CreateLock(string name)
            => new SqlDistributedLock(name, SqlDistributedLockTest.ConnectionString, SqlDistributedLockConnectionStrategy.Azure);

        internal override string GetSafeLockName(string name) => SqlDistributedLock.GetSafeLockName(name);
    }
}
