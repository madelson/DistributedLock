using Medallion.Threading.Sql.ConnectionMultiplexing;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Sql
{
    public abstract class MultiplexingConnectionStrategyTestCases<TEngineFactory> : TestBase
        where TEngineFactory : ITestingSqlDistributedLockEngineFactory, new()
    {
        /// <summary>
        /// Similar to <see cref="DistributedLockCoreTestCases{TEngine}.TestLockAbandonment"/> but demonstrates 
        /// the time-based cleanup loop rather than forcing a cleanup
        /// </summary>
        [Test]
        public void TestLockAbandonmentWithTimeBasedCleanupRun()
        {
            using (var engine = new TEngineFactory().Create<MultiplexedConnectionStringProvider>())
            {
                var originalInterval = MultiplexedConnectionLockPool.CleanupIntervalSeconds;
                MultiplexedConnectionLockPool.CleanupIntervalSeconds = 1;
                try
                {
                    var lock1 = engine.CreateLock(nameof(TestCleanup));
                    var lock2 = engine.CreateLock(nameof(TestCleanup));
                    var handleReference = this.TestCleanupHelper(lock1, lock2);

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    handleReference.IsAlive.ShouldEqual(false);

                    // we attempt to shorten the interval. However if we are already in the midst of sleeping
                    // when we get to this test this won't do any good. Therefore we need to wait up to the
                    // twice the original interval to guarantee completion
                    var maxWait = TimeSpan.FromSeconds(2 * Math.Max(originalInterval, MultiplexedConnectionLockPool.CleanupIntervalSeconds));
                    var stopwatch = Stopwatch.StartNew();
                    while (true)
                    {
                        using (var handle = lock2.TryAcquire())
                        {
                            if (handle != null) { break; }
                        }
                        if (stopwatch.Elapsed > maxWait)
                        {
                            Assert.Fail(this.GetType().Name);
                        }
                        Thread.Sleep(TimeSpan.FromSeconds(.25));
                    }
                }
                finally
                {
                    MultiplexedConnectionLockPool.CleanupIntervalSeconds = originalInterval;
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // need to isolate for GC
        private WeakReference TestCleanupHelper(IDistributedLock lock1, IDistributedLock lock2)
        {
            var handle = lock1.Acquire();

            Assert.IsNull(lock2.TryAcquireAsync().Result);

            return new WeakReference(handle);
        }

        /// <summary>
        /// This method demonstrates how <see cref="SqlDistributedLockConnectionStrategy.OptimisticConnectionMultiplexing"/>
        /// can be used to hold many locks concurrently on one underlying connection.
        /// 
        /// Note: I would like this test to actually leverage multiple threads, but this runs into issues because the current
        /// implementation of optimistic multiplexing only makes one attempt to use a shared lock before opening a new connection.
        /// This runs into problems because the attempt to use a shared lock can fail if, for example, a lock is being released on
        /// that connection which means that the mutex for the connection can't be acquired without waiting. Once something like
        /// this happens, we try to open a new connection which times out due to pool size limits
        /// </summary>
        [Test]
        public void TestHighConcurrencyWithSmallPool()
        {
            var connectionString = new SqlConnectionStringBuilder(ConnectionStringProvider.ConnectionString) { MaxPoolSize = 1 }.ConnectionString;

            async Task Test()
            {
                using (var engine = new TEngineFactory().Create<MultiplexedConnectionStringProvider>())
                {
                    var random = new Random(12345);

                    var heldLocks = new Dictionary<string, IDisposable>();
                    for (var i = 0; i < 1000; ++i)
                    {
                        var lockName = $"{nameof(TestHighConcurrencyWithSmallPool)}_{random.Next(20)}";
                        if (heldLocks.TryGetValue(lockName, out var existingHandle))
                        {
                            existingHandle.Dispose();
                            heldLocks.Remove(lockName);
                        }
                        else
                        {
                            var @lock = engine.CreateLock(lockName);
                            var handle = await @lock.TryAcquireAsync();
                            if (handle != null) { heldLocks.Add(lockName, handle); }
                        }
                    }
                }
            };

            Task.Run(Test).Wait(TimeSpan.FromSeconds(10)).ShouldEqual(true);
        }
    }
}
