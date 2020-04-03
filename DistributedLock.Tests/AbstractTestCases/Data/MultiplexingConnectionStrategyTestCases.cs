using Medallion.Threading.Internal;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Data
{
    public abstract class MultiplexingConnectionStrategyTestCases<TLockProvider, TDb>
        where TLockProvider : TestingLockProvider<TestingConnectionMultiplexingSynchronizationStrategy<TDb>>, new()
        where TDb : ITestingPrimaryClientDb, new()
    {
        private TLockProvider _lockProvider = default!;

        [SetUp] public void SetUp() => this._lockProvider = new TLockProvider();
        [TearDown] public void TearDown() => this._lockProvider.Dispose();

        /// <summary>
        /// Similar to <see cref="DistributedLockCoreTestCases{TLockProvider}.TestLockAbandonment"/> but demonstrates 
        /// the time-based cleanup loop rather than forcing a cleanup
        /// </summary>
        [Test]
        // todo if we parallelize we need to make sure this test blocks anyone else from calling MFQ.FinalizeAsync() since that defeats the point
        public void TestLockAbandonmentWithTimeBasedCleanupRun()
        {
            var lock1 = this._lockProvider.CreateLock(nameof(this.TestLockAbandonmentWithTimeBasedCleanupRun));
            var lock2 = this._lockProvider.CreateLock(nameof(this.TestLockAbandonmentWithTimeBasedCleanupRun));
            var handleReference = this.TestCleanupHelper(lock1, lock2);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            handleReference.IsAlive.ShouldEqual(false);

            // We might get lucky and wait for less than the cadence based on how the timing works out. However,
            // due to system load we might also need to wait longer than the cadence. To be safe, we wait for up
            // to 2x the cadence but check in frequently to see if we can finish early.
            var maxWait = TimeSpan.FromSeconds(2 * ManagedFinalizerQueue.FinalizerCadence.TotalSeconds);
            var stopwatch = Stopwatch.StartNew();
            while (lock2.IsHeld())
            {
                if (stopwatch.Elapsed > maxWait)
                {
                    Assert.Fail(this.GetType().ToString());
                }
                Thread.Sleep(TimeSpan.FromSeconds(.25));
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
        /// This method demonstrates how multiplexing can be used to hold many locks concurrently on one underlying connection.
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
            const int LockNameCount = 20;

            // Pre-generate all locks we will use. This is necessary for our Semaphore5 strategy, where the first lock created
            // takes 4 of the 5 tickets (and thus may need more connections than a single-connection pool can support). For other
            // lock types this does nothing since creating a lock might open a connection but otherwise won't run any commands
            for (var i = 0; i < LockNameCount; ++i)
            {
                this._lockProvider.CreateLock(MakeLockName(i));
            }

            // Multiplexing is not allowed for upgrade locks since the upgrade operation could block. Therefore
            // we don't allow a lock provider based on a RW lock to use its upgrade lock as an exclusive lock
            if (this._lockProvider is ITestingReaderWriterLockAsMutexProvider readerWriterAsMutexProvider)
            {
                readerWriterAsMutexProvider.DisableUpgradeLock = true;
            }

            // assign a unique app name to make sure we'll own the entire pool
            this._lockProvider.Strategy.SetUniqueApplicationName();
            this._lockProvider.Strategy.Db.MaxPoolSize = 1;

            async Task Test()
            {
                var random = new Random(12345);

                var heldLocks = new Dictionary<string, IDisposable>();
                for (var i = 0; i < 1000; ++i)
                {
                    var lockName = MakeLockName(random.Next(20));
                    if (heldLocks.TryGetValue(lockName, out var existingHandle))
                    {
                        existingHandle.Dispose();
                        heldLocks.Remove(lockName);
                    }
                    else
                    {
                        var @lock = this._lockProvider.CreateLock(lockName);
                        var handle = await @lock.TryAcquireAsync();
                        if (handle != null) { heldLocks.Add(lockName, handle); }
                    }
                }

                foreach (var remainingHandle in heldLocks.Values)
                {
                    remainingHandle.Dispose();
                }
            };

            Assert.IsTrue(Task.Run(Test).Wait(Debugger.IsAttached ? TimeSpan.FromMinutes(10) : TimeSpan.FromSeconds(10)));

            string MakeLockName(int i) => $"{nameof(TestHighConcurrencyWithSmallPool)}_{i}";
        }
    }
}
