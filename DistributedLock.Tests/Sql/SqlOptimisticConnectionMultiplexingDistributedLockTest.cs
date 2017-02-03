using Medallion.Threading.Sql;
using Medallion.Threading.Sql.ConnectionMultiplexing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Sql
{
    [TestClass]
    public class SqlOptimisticConnectionMultiplexingDistributedLockTest : DistributedLockTestBase
    {
        [TestMethod]
        public void TestCleanup()
        {
            var originalInterval = MultiplexedConnectionLockPool.CleanupIntervalSeconds;
            MultiplexedConnectionLockPool.CleanupIntervalSeconds = 1;
            try
            {
                var lock1 = this.CreateLock(nameof(TestCleanup));
                var lock2 = this.CreateLock(nameof(TestCleanup));
                var handleReference = this.TestCleanupHelper(lock1, lock2);
                
                GC.Collect();
                GC.WaitForPendingFinalizers();
                handleReference.IsAlive.ShouldEqual(false);
                Thread.Sleep(TimeSpan.FromSeconds(5));

                using (var handle = lock2.TryAcquire())
                {
                    Assert.IsNotNull(handle);
                }
            }
            finally
            {
                MultiplexedConnectionLockPool.CleanupIntervalSeconds = originalInterval;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // need to isolate for GC
        private WeakReference TestCleanupHelper(IDistributedLock lock1, IDistributedLock lock2)
        {
            var handle = lock1.Acquire();

            Assert.IsNull(lock2.TryAcquireAsync().Result);

            return new WeakReference(handle);
        }

        internal override IDistributedLock CreateLock(string name)
            => new SqlDistributedLock(name, SqlDistributedLockTest.ConnectionString, SqlDistributedLockConnectionStrategy.OptimisticConnectionMultiplexing);

        internal override string GetSafeLockName(string name) => SqlDistributedLock.GetSafeLockName(name);

        /// <summary>
        /// The default abandonment test doesn't work with multiplexing because the cleanup timer must come
        /// around. <see cref="TestCleanup"/> demonstrates this functionality instead
        /// </summary>
        internal override bool SupportsInProcessAbandonment => false;
    }
}
