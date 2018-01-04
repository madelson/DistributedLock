using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Medallion.Threading.Sql;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;

namespace Medallion.Threading.Tests.Sql
{
    [TestClass]
    public sealed class SqlDistributedReaderWriterLockTest : SqlDistributedReaderWriterLockTestBase
    {
        [TestMethod]
        public void TestBadConstructorArguments()
        {
            TestHelper.AssertThrows<ArgumentNullException>(() => new SqlDistributedReaderWriterLock(null, SqlDistributedLockTest.ConnectionString));
            TestHelper.AssertThrows<ArgumentNullException>(() => new SqlDistributedReaderWriterLock("a", default(string)));
            TestHelper.AssertThrows<ArgumentNullException>(() => new SqlDistributedReaderWriterLock("a", default(DbTransaction)));
            TestHelper.AssertThrows<ArgumentNullException>(() => new SqlDistributedReaderWriterLock("a", default(DbConnection)));
            TestHelper.AssertThrows<FormatException>(() => new SqlDistributedReaderWriterLock(new string('a', SqlDistributedReaderWriterLock.MaxLockNameLength + 1), SqlDistributedLockTest.ConnectionString));
            TestHelper.AssertDoesNotThrow(() => new SqlDistributedReaderWriterLock(new string('a', SqlDistributedReaderWriterLock.MaxLockNameLength), SqlDistributedLockTest.ConnectionString));
        }

        [TestMethod]
        public void TestGetSafeLockNameCompat()
        {
            SqlDistributedReaderWriterLock.MaxLockNameLength.ShouldEqual(SqlDistributedLock.MaxLockNameLength);

            var cases = new[] 
            {
                string.Empty,
                "abc",
                "\\",
                new string('a', SqlDistributedLock.MaxLockNameLength),
                new string('\\', SqlDistributedLock.MaxLockNameLength),
                new string('x', SqlDistributedLock.MaxLockNameLength + 1)
            };

            foreach (var lockName in cases)
            {
                // should be compatible with SqlDistributedLock
                this.GetSafeLockName(lockName).ShouldEqual(SqlDistributedLock.GetSafeLockName(lockName));
            }
        }
        
        internal override SqlDistributedReaderWriterLock CreateReaderWriterLock(string name) =>
            new SqlDistributedReaderWriterLock(name, SqlDistributedLockTest.ConnectionString);
    }

    [TestClass]
    public sealed class SqlOwnedConnectionDistributedReaderWriterLockTest : SqlDistributedReaderWriterLockTestBase
    {
        internal override SqlDistributedReaderWriterLock CreateReaderWriterLock(string name) =>
            new SqlDistributedReaderWriterLock(name, SqlDistributedLockTest.ConnectionString, SqlDistributedLockConnectionStrategy.Connection);

        internal override bool UseWriteLockAsExclusive() => false; // random choice
    }

    [TestClass]
    public sealed class SqlOwnedTransactionDistributedReaderWriterLockTest : SqlDistributedReaderWriterLockTestBase
    {
        internal override SqlDistributedReaderWriterLock CreateReaderWriterLock(string name) =>
            new SqlDistributedReaderWriterLock(name, SqlDistributedLockTest.ConnectionString, SqlDistributedLockConnectionStrategy.Transaction);

        internal override bool UseWriteLockAsExclusive() => true; // random choice
    }

    [TestClass]
    public sealed class SqlOptimisticConnectionPoolingDistributedReaderWriterLockTest : SqlDistributedReaderWriterLockTestBase
    {
        internal override SqlDistributedReaderWriterLock CreateReaderWriterLock(string name) =>
            new SqlDistributedReaderWriterLock(name, SqlDistributedLockTest.ConnectionString, SqlDistributedLockConnectionStrategy.OptimisticConnectionMultiplexing);

        internal override bool UseWriteLockAsExclusive() => false; // random choice
    }

    [TestClass]
    public sealed class SqlConnectionScopedDistributedReaderWriterLockTest : SqlDistributedReaderWriterLockTestBase
    {
        [TestMethod]
        public void TestCloseLockOnClosedConnection()
        {
            using (var connection = new SqlConnection(SqlDistributedLockTest.ConnectionString))
            {
                TestHelper.AssertThrows<InvalidOperationException>(() => new SqlDistributedReaderWriterLock("a", connection).AcquireWriteLock());

                connection.Open();

                var @lock = new SqlDistributedReaderWriterLock("a", connection);
                var handle = @lock.AcquireWriteLock();
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

                using (var handle = new SqlDistributedReaderWriterLock("a", connection).AcquireWriteLock())
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        transaction.Rollback();
                    }

                    SqlTransactionScopedDistributedLockTest.IsHeld("a").ShouldEqual(true);
                }
            }
        }

        internal override SqlDistributedReaderWriterLock CreateReaderWriterLock(string name)
        {
            // ensures that we don't run out of connections
            SqlConnection.ClearAllPools();

            var connection = new SqlConnection(SqlDistributedLockTest.ConnectionString);
            connection.Open();
            this.AddCleanupAction(connection.Dispose);
            return new SqlDistributedReaderWriterLock(name, connection);
        }

        internal override bool IsReentrant => true;
        // todo is this just because of the cleanup action?
        // from my testing, it appears that abandoning a SqlConnection does not cause it to be released
        internal override bool SupportsInProcessAbandonment => false;

        internal override bool UseWriteLockAsExclusive() => true; // random choice
    }

    [TestClass]
    public sealed class SqlTransactionScopedDistributedReaderWriterLockTest : SqlDistributedReaderWriterLockTestBase
    {
        [TestMethod]
        public void TestScopedToTransactionOnly()
        {
            using (var connection = new SqlConnection(SqlDistributedLockTest.ConnectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    using (new SqlDistributedReaderWriterLock("t", transaction).AcquireWriteLock())
                    {
                        using (var handle = new SqlDistributedReaderWriterLock("t", transaction).TryAcquireWriteLock())
                        {
                            Assert.IsNotNull(handle, "reentrant");
                        }

                        TestHelper.AssertThrows<InvalidOperationException>(() => new SqlDistributedReaderWriterLock("t", connection).TryAcquireWriteLock());
                    }
                }
            }
        }

        [TestMethod]
        public void TestCloseLockOnClosedConnection()
        {
            using (var connection = new SqlConnection(SqlDistributedLockTest.ConnectionString))
            {
                TestHelper.AssertThrows<InvalidOperationException>(() => new SqlDistributedReaderWriterLock("a", connection).AcquireWriteLock());

                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    var @lock = new SqlDistributedReaderWriterLock("a", transaction);
                    var handle = @lock.AcquireWriteLock();
                    SqlTransactionScopedDistributedLockTest.IsHeld("a").ShouldEqual(true);

                    connection.Dispose();

                    TestHelper.AssertDoesNotThrow(handle.Dispose);

                    // lock can be re-acquired
                    SqlTransactionScopedDistributedLockTest.IsHeld("a").ShouldEqual(false);
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
                    handle = new SqlDistributedReaderWriterLock("b", transaction).AcquireWriteLock();
                    SqlTransactionScopedDistributedLockTest.IsHeld("b").ShouldEqual(true);
                }
                TestHelper.AssertDoesNotThrow(handle.Dispose);
                SqlTransactionScopedDistributedLockTest.IsHeld("b").ShouldEqual(false);
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
                    var handle = new SqlDistributedReaderWriterLock("c", transaction).AcquireWriteLock();
                    SqlTransactionScopedDistributedLockTest.IsHeld("c").ShouldEqual(true);

                    transaction.Rollback();

                    TestHelper.AssertDoesNotThrow(handle.Dispose);
                    SqlTransactionScopedDistributedLockTest.IsHeld("c").ShouldEqual(false);

                    TestHelper.AssertThrows<InvalidOperationException>(() => new SqlDistributedReaderWriterLock("c", transaction).AcquireWriteLock());
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
                    var handle = new SqlDistributedReaderWriterLock("d", transaction).AcquireWriteLock();
                    SqlTransactionScopedDistributedLockTest.IsHeld("d").ShouldEqual(true);

                    transaction.Commit();

                    TestHelper.AssertDoesNotThrow(handle.Dispose);
                    SqlTransactionScopedDistributedLockTest.IsHeld("d").ShouldEqual(false);

                    TestHelper.AssertThrows<InvalidOperationException>(() => new SqlDistributedReaderWriterLock("d", transaction).AcquireWriteLock());
                }
            }
        }

        internal override SqlDistributedReaderWriterLock CreateReaderWriterLock(string name)
        {
            // ensures that we don't run out of connections
            SqlConnection.ClearAllPools();

            var connection = new SqlConnection(SqlDistributedLockTest.ConnectionString);
            connection.Open();
            this.AddCleanupAction(connection.Dispose);
            return new SqlDistributedReaderWriterLock(name, connection.BeginTransaction());
        }

        internal override bool IsReentrant => true;
        // todo is this just because of the cleanup action?
        // from my testing, it appears that abandoning a SqlTransaction does not cause it to be released
        internal override bool SupportsInProcessAbandonment => false;

        internal override bool UseWriteLockAsExclusive() => false; // random choice
    }

    [TestClass]
    public class SqlAzureDistributedReaderWriterLockTestBase : SqlDistributedReaderWriterLockTestBase
    {
        [TestMethod]
        public void TestAzureStrategyProtectsFromIdleSessionKiller()
        {
            var originalInterval = KeepaliveHelper.Interval;
            try
            {
                KeepaliveHelper.Interval = TimeSpan.FromSeconds(.1);

                using (var idleSessionKiller = new IdleSessionKiller(SqlDistributedLockTest.ConnectionString, idleTimeout: TimeSpan.FromSeconds(.25)))
                {
                    var @lock = this.CreateReaderWriterLock(nameof(TestAzureStrategyProtectsFromIdleSessionKiller));
                    var handle = @lock.AcquireReadLock();
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
        /// Tests the logic where upgrading a connection stops and restarts the keepalive
        /// </summary>
        [TestMethod]
        public void TestAzureStrategyProtectsFromIdleSessionKillerAfterFailedUpgrade()
        {
            var originalInterval = KeepaliveHelper.Interval;
            try
            {
                KeepaliveHelper.Interval = TimeSpan.FromSeconds(.1);

                var readLockHolder = this.CreateReaderWriterLock(nameof(TestAzureStrategyProtectsFromIdleSessionKillerAfterFailedUpgrade));
                using (new IdleSessionKiller(SqlDistributedLockTest.ConnectionString, idleTimeout: TimeSpan.FromSeconds(.25)))
                using (readLockHolder.AcquireReadLock())
                {
                    var @lock = this.CreateReaderWriterLock(nameof(TestAzureStrategyProtectsFromIdleSessionKillerAfterFailedUpgrade));
                    var handle = @lock.AcquireUpgradeableReadLock();
                    handle.TryUpgradeToWriteLock().ShouldEqual(false);
                    handle.TryUpgradeToWriteLockAsync().Result.ShouldEqual(false);
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
                    var @lock = this.CreateReaderWriterLock(nameof(ThreadSafetyExercise));
                    for (var i = 0; i < 100; ++i)
                    {
                        using (var handle = @lock.AcquireUpgradeableReadLockAsync().Result)
                        {
                            Thread.Sleep(1);
                            handle.UpgradeToWriteLock();
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

        internal override SqlDistributedReaderWriterLock CreateReaderWriterLock(string name)
        {
            return new SqlDistributedReaderWriterLock(name, SqlDistributedLockTest.ConnectionString, SqlDistributedLockConnectionStrategy.Azure);
        }
    }
}
