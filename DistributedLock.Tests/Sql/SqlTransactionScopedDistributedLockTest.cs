using Medallion.Threading.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Sql
{
    [TestClass]
    public class SqlTransactionScopedDistributedLockTest : DistributedLockTestBase
    {
        [TestMethod]
        public void TestScopedToTransactionOnly()
        {
            using (var connection = new SqlConnection(SqlDistributedLockTest.ConnectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    using (new SqlDistributedLock("t", transaction).Acquire())
                    {
                        using (var handle = new SqlDistributedLock("t", transaction).TryAcquire())
                        {
                            Assert.IsNotNull(handle, "reentrant");
                        }

                        TestHelper.AssertThrows<InvalidOperationException>(() => new SqlDistributedLock("t", connection).TryAcquire());
                    }
                }
            }
        }

        [TestMethod]
        public void TestCloseLockOnClosedConnection()
        {
            using (var connection = new SqlConnection(SqlDistributedLockTest.ConnectionString))
            {
                TestHelper.AssertThrows<InvalidOperationException>(() => new SqlDistributedLock("a", connection).Acquire());

                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    var @lock = new SqlDistributedLock("a", transaction);
                    var handle = @lock.Acquire();
                    IsHeld("a").ShouldEqual(true);

                    connection.Dispose();

                    TestHelper.AssertDoesNotThrow(handle.Dispose);

                    // lock can be re-acquired
                    IsHeld("a").ShouldEqual(false);
                }
            }
        }

        [TestMethod]
        public void TestLockOnClosedTransaction()
        {
            using (var connection = new SqlConnection(SqlDistributedLockTest.ConnectionString))
            {
                connection.Open();

                IDisposable handle;
                using (var transaction = connection.BeginTransaction())
                {
                    handle = new SqlDistributedLock("b", transaction).Acquire();
                    IsHeld("b").ShouldEqual(true);
                }
                TestHelper.AssertDoesNotThrow(handle.Dispose);
                IsHeld("b").ShouldEqual(false);
            }
        }

        [TestMethod]
        public void TestLockOnRolledBackTransaction()
        {
            using (var connection = new SqlConnection(SqlDistributedLockTest.ConnectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    var handle = new SqlDistributedLock("c", transaction).Acquire();
                    IsHeld("c").ShouldEqual(true);

                    transaction.Rollback();

                    TestHelper.AssertDoesNotThrow(handle.Dispose);
                    IsHeld("c").ShouldEqual(false);

                    TestHelper.AssertThrows<InvalidOperationException>(() => new SqlDistributedLock("c", transaction).Acquire());
                }
            }
        }

        [TestMethod]
        public void TestLockOnCommittedBackTransaction()
        {
            using (var connection = new SqlConnection(SqlDistributedLockTest.ConnectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    var handle = new SqlDistributedLock("d", transaction).Acquire();
                    IsHeld("d").ShouldEqual(true);

                    transaction.Commit();

                    TestHelper.AssertDoesNotThrow(handle.Dispose);
                    IsHeld("d").ShouldEqual(false);

                    TestHelper.AssertThrows<InvalidOperationException>(() => new SqlDistributedLock("d", transaction).Acquire());
                }
            }
        }

        internal static bool IsHeld(string lockName)
        {
            using (var handle = new SqlDistributedLock(lockName, SqlDistributedLockTest.ConnectionString).TryAcquire())
            {
                return handle == null;
            }
        }

        internal override IDistributedLock CreateLock(string name)
        {
            // ensures that we don't run out of connections
            SqlConnection.ClearAllPools();

            var connection = new SqlConnection(SqlDistributedLockTest.ConnectionString);
            connection.Open();
            var transaction = connection.BeginTransaction();
            this.AddCleanupAction(connection.Dispose);
            return new SqlDistributedLock(name, transaction);
        }

        internal override string GetSafeLockName(string name)
        {
            return SqlDistributedLock.GetSafeLockName(name);
        }

        internal override bool IsReentrant { get { return true; } }
    }
}
