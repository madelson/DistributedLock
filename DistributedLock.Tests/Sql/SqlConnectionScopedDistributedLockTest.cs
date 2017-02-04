using Medallion.Threading.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Sql
{
    [TestClass]
    public class SqlConnectionScopedDistributedLockTest : DistributedLockTestBase
    {
        [TestMethod]
        public void TestCloseLockOnClosedConnection()
        {
            using (var connection = new SqlConnection(SqlDistributedLockTest.ConnectionString))
            {
                TestHelper.AssertThrows<InvalidOperationException>(() => new SqlDistributedLock("a", connection).Acquire());

                connection.Open();

                var @lock = new SqlDistributedLock("a", connection);
                var handle = @lock.Acquire();
                SqlTransactionScopedDistributedLockTest.IsHeld("a").ShouldEqual(true);

                connection.Dispose();

                TestHelper.AssertDoesNotThrow(handle.Dispose);

                // lock can be re-acquired
                SqlTransactionScopedDistributedLockTest.IsHeld("a").ShouldEqual(false);
            }
        }

        [TestMethod]
        public void TestIsNotScopedToTransaction()
        {
            using (var connection = new SqlConnection(SqlDistributedLockTest.ConnectionString))
            {
                connection.Open();

                using (var handle = new SqlDistributedLock("a", connection).Acquire())
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        transaction.Rollback();
                    }

                    SqlTransactionScopedDistributedLockTest.IsHeld("a").ShouldEqual(true);
                }
            }
        }

        internal override IDistributedLock CreateLock(string name)
        {
            // ensures that we don't run out of connections
            SqlConnection.ClearAllPools();

            var connection = new SqlConnection(SqlDistributedLockTest.ConnectionString);
            connection.Open();
            this.AddCleanupAction(connection.Dispose);
            return new SqlDistributedLock(name, connection);
        }

        internal override string GetSafeLockName(string name)
        {
            return SqlDistributedLock.GetSafeLockName(name);
        }

        internal override bool IsReentrant => true;
        // from my testing, it appears that abandoning a SqlConnection does not cause it to be released
        internal override bool SupportsInProcessAbandonment => false;
    }
}
