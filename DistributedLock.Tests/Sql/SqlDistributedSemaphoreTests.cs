using Medallion.Threading.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Sql
{
    [TestClass]
    public sealed class SqlDistributedSemaphoreTest : SqlDistributedSemaphoreTestBase
    {
        [TestMethod]
        public void TestBadConstructorArguments()
        {
            TestHelper.AssertThrows<ArgumentNullException>(() => new SqlDistributedSemaphore(null, 1, SqlDistributedLockTest.ConnectionString));
            TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => new SqlDistributedSemaphore("a", -1, SqlDistributedLockTest.ConnectionString));
            TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => new SqlDistributedSemaphore("a", 0, SqlDistributedLockTest.ConnectionString));
            TestHelper.AssertThrows<ArgumentNullException>(() => new SqlDistributedSemaphore("a", 1, default(string)));
            TestHelper.AssertThrows<ArgumentNullException>(() => new SqlDistributedSemaphore("a", 1, default(IDbConnection)));
            TestHelper.AssertThrows<ArgumentNullException>(() => new SqlDistributedSemaphore("a", 1, default(IDbTransaction)));
            TestHelper.AssertThrows<ArgumentException>(() => new SqlDistributedSemaphore("a", 1, SqlDistributedLockTest.ConnectionString, (SqlDistributedLockConnectionStrategy)int.MinValue));

            var random = new Random(1234);
            var bytes = new byte[10000];
            random.NextBytes(bytes);
            TestHelper.AssertDoesNotThrow(() => new SqlDistributedSemaphore(Encoding.UTF8.GetString(bytes), int.MaxValue, SqlDistributedLockTest.ConnectionString));
        }

        // todo add compat tests for name mangling
    }

    // todo consider including other connection strategy-specific tests via new base tests (e. g. in the SDL or SDRWL test classes).
    // We can do this via making test base classes testcase only and moving setup/customization to "engine" classes provided by generic arguments
    // or to protected set properties that the constructor can set

    /// <summary>
    /// Tests using a mostly-drained multi-ticket semaphore as a mutex
    /// </summary>
    [TestClass]
    public sealed class MostlyDrainedSqlDistributedSemaphoreTest : DistributedLockTestBase
    {
        private static readonly HashSet<string> MostlyDrainedSemaphores = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private const int MaxCount = 10;

        internal override IDistributedLock CreateLock(string name)
        {
            var semaphore = new SqlDistributedSemaphore(name, MaxCount, SqlDistributedLockTest.ConnectionString);
            lock (MostlyDrainedSemaphores)
            {
                if (MostlyDrainedSemaphores.Add(name))
                {
                    var handles = Enumerable.Range(0, MaxCount - 1).Select(_ => semaphore.Acquire(TimeSpan.FromSeconds(10)))
                        .ToList();
                    this.AddCleanupAction(() =>
                    {
                        handles.ForEach(h => h.Dispose());
                        lock (MostlyDrainedSemaphores)
                        {
                            MostlyDrainedSemaphores.Remove(name);
                        }
                    });
                }
            }

            return semaphore;
        }

        internal override string CrossProcessLockType => base.CrossProcessLockType + MaxCount;

        internal override string GetSafeLockName(string name) => name ?? throw new ArgumentNullException(nameof(name));
    }

    [TestClass]
    public sealed class SqlDistributedSemaphoreOwnedConnectionTest : SqlDistributedSemaphoreTestBase
    {
        protected override SqlDistributedSemaphore CreateSemaphore(string name, int maxCount) =>
            new SqlDistributedSemaphore(name, maxCount, SqlDistributedLockTest.ConnectionString, SqlDistributedLockConnectionStrategy.Connection);
    }

    [TestClass]
    public sealed class SqlDistributedSemaphoreOwnedTransactionTest : SqlDistributedSemaphoreTestBase
    {
        protected override SqlDistributedSemaphore CreateSemaphore(string name, int maxCount) =>
            new SqlDistributedSemaphore(name, maxCount, SqlDistributedLockTest.ConnectionString, SqlDistributedLockConnectionStrategy.Transaction);
    }

    [TestClass]
    public sealed class SqlDistributedSemaphoreOptimisticConnectionMultiplexingTest : SqlDistributedSemaphoreTestBase
    {
        protected override SqlDistributedSemaphore CreateSemaphore(string name, int maxCount) =>
            new SqlDistributedSemaphore(name, maxCount, SqlDistributedLockTest.ConnectionString, SqlDistributedLockConnectionStrategy.OptimisticConnectionMultiplexing);

        // todo this is because we don't set the cleanup frequency; we could fix by making the LockAbandonment tests call before/after methods that we can override
        internal override bool SupportsInProcessAbandonment => false;
    }

    [TestClass]
    public sealed class SqlDistributedSemaphoreAzureTest : SqlDistributedSemaphoreTestBase
    {
        protected override SqlDistributedSemaphore CreateSemaphore(string name, int maxCount) =>
            new SqlDistributedSemaphore(name, maxCount, SqlDistributedLockTest.ConnectionString, SqlDistributedLockConnectionStrategy.Azure);
    }

    [TestClass]
    public sealed class SqlDistributedSemaphoreConnectionScopedTest : SqlDistributedSemaphoreTestBase
    {
        protected override SqlDistributedSemaphore CreateSemaphore(string name, int maxCount)
        {
            // todo common helper here?

            // ensures that we don't run out of connections
            SqlConnection.ClearAllPools();

            var connection = new SqlConnection(SqlDistributedLockTest.ConnectionString);
            connection.Open();
            this.AddCleanupAction(connection.Dispose);
            return new SqlDistributedSemaphore(name, maxCount, connection);
        }

        // from my testing, it appears that abandoning a SqlConnection does not cause it to be released
        internal override bool SupportsInProcessAbandonment => false;
    }

    [TestClass]
    public sealed class SqlDistributedSemaphoreTransactionScopedTest : SqlDistributedSemaphoreTestBase
    {
        protected override SqlDistributedSemaphore CreateSemaphore(string name, int maxCount)
        {
            // ensures that we don't run out of connections
            SqlConnection.ClearAllPools();

            var connection = new SqlConnection(SqlDistributedLockTest.ConnectionString);
            connection.Open();
            this.AddCleanupAction(connection.Dispose);
            return new SqlDistributedSemaphore(name, maxCount, connection.BeginTransaction());
        }

        // from my testing, it appears that abandoning a SqlTransaction does not cause it to be released
        internal override bool SupportsInProcessAbandonment => false;
    }
}
