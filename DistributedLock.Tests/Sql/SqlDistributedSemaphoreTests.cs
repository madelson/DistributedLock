using Medallion.Threading.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Sql
{
    [TestClass]
    public class SqlDistributedSemaphoreTest : SqlDistributedSemaphoreTestBase
    {
        [TestMethod]
        public void TestBadConstructorArguments()
        {
            TestHelper.AssertThrows<ArgumentNullException>(() => new SqlDistributedSemaphore(null, 1, SqlDistributedLockTest.ConnectionString));
            TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => new SqlDistributedSemaphore("a", -1, SqlDistributedLockTest.ConnectionString));
            TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => new SqlDistributedSemaphore("a", 0, SqlDistributedLockTest.ConnectionString));
            TestHelper.AssertThrows<ArgumentNullException>(() => new SqlDistributedSemaphore("a", 1, null));

            var random = new Random(1234);
            var bytes = new byte[10000];
            random.NextBytes(bytes);
            TestHelper.AssertDoesNotThrow(() => new SqlDistributedSemaphore(Encoding.UTF8.GetString(bytes), int.MaxValue, SqlDistributedLockTest.ConnectionString));
            // todo add more here
        }

        // todo add compat tests for name mangling
    }

    /// <summary>
    /// Tests using a mostly-drained multi-ticket semaphore as a mutex
    /// </summary>
    [TestClass]
    public class MostlyDrainedSqlDistributedSemaphoreTest : SqlDistributedSemaphoreTestBase
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
    }

    // todo add tests for other connection methods
}
