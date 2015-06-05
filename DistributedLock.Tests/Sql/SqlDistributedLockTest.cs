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
    public class SqlDistributedLockTest
    {
        private static readonly string ConnectionString = new SqlConnectionStringBuilder
            {
                DataSource = @".\SQLEXPRESS",
                InitialCatalog = "master",
                IntegratedSecurity = true
            }
            .ConnectionString;

        [TestMethod]
        public void BasicTest()
        {
            var @lock = new SqlDistributedLock("ralph", ConnectionString);
            var lock2 = new SqlDistributedLock("ralph2", ConnectionString);

            using (var handle = @lock.TryAcquire())
            {
                Assert.IsNotNull(handle);

                using (var nestedHandle = @lock.TryAcquire())
                {
                    Assert.IsNull(nestedHandle);
                }

                using (var nestedHandle2 = lock2.TryAcquire())
                {
                    Assert.IsNotNull(nestedHandle2);
                }
            }

            using (var handle = @lock.TryAcquire())
            {
                Assert.IsNotNull(handle);
            }
        }

        [TestMethod]
        public void TestBadArguments()
        {
            TestHelper.AssertThrows<ArgumentNullException>(() => new SqlDistributedLock(null, ConnectionString));
            TestHelper.AssertThrows<ArgumentNullException>(() => new SqlDistributedLock("a", null));
            TestHelper.AssertThrows<FormatException>(() => new SqlDistributedLock(new string('a', SqlDistributedLock.MaxLockNameLength + 1), ConnectionString));
            TestHelper.AssertDoesNotThrow(() => new SqlDistributedLock(new string('a', SqlDistributedLock.MaxLockNameLength), ConnectionString));

            var @lock = new SqlDistributedLock(Guid.NewGuid().ToString(), ConnectionString);
            TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.Acquire(TimeSpan.FromSeconds(-2)));
            TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.AcquireAsync(TimeSpan.FromSeconds(-2)));
            TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.TryAcquire(TimeSpan.FromSeconds(-2)));
            TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.TryAcquireAsync(TimeSpan.FromSeconds(-2)));
            TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.Acquire(TimeSpan.FromSeconds(int.MaxValue)));
            TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.AcquireAsync(TimeSpan.FromSeconds(int.MaxValue)));
            TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.TryAcquire(TimeSpan.FromSeconds(int.MaxValue)));
            TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.TryAcquireAsync(TimeSpan.FromSeconds(int.MaxValue)));
        }

        [TestMethod]
        public void CancellationTest()
        {
            var @lock = new SqlDistributedLock("gerald", ConnectionString);

            var source = new CancellationTokenSource(); 
            using (var handle = @lock.Acquire())
            {
                var blocked = @lock.AcquireAsync(cancellationToken: source.Token);
                blocked.Wait(TimeSpan.FromSeconds(.1)).ShouldEqual(false);
                source.Cancel();
                blocked.ContinueWith(_ => { }).Wait(TimeSpan.FromSeconds(10)).ShouldEqual(true);
                blocked.Status.ShouldEqual(TaskStatus.Canceled, (blocked.Exception ?? (object)"no exception").ToString());
            }

            // already canceled
            source = new CancellationTokenSource();
            source.Cancel();
            TestHelper.AssertThrows<OperationCanceledException>(() => @lock.Acquire(cancellationToken: source.Token));
        }

        [TestMethod]
        public void TestGarbageCollection()
        {
            var @lock = new SqlDistributedLock("gc_test", ConnectionString);
            Func<WeakReference> abandonLock = () => new WeakReference(@lock.Acquire());

            var weakHandle = abandonLock();
            for (var i = 0; i < 1; ++i)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            // this is needed because the pool reclaims the SqlConnection but doesn't close it
            SqlConnection.ClearAllPools();

            weakHandle.IsAlive.ShouldEqual(false);
            using (var handle = @lock.TryAcquire())
            {
                Assert.IsNotNull(handle);
            }
        }

        [TestMethod]
        public void TestParallelism()
        {
            var counter = 0;
            var tasks = Enumerable.Range(1, 100).Select(async _ =>
                {
                    var @lock = new SqlDistributedLock("parallel_test", ConnectionString);
                    using (await @lock.AcquireAsync())
                    {
                        // increment going in
                        Interlocked.Increment(ref counter);

                        // hang out for a bit to ensure concurrency
                        await Task.Delay(TimeSpan.FromMilliseconds(10));

                        // decrement and return on the way out (returns # inside the lock when this left ... should be 0)
                        return Interlocked.Decrement(ref counter);
                    }
                })
                .ToList();

            Task.WaitAll(tasks.ToArray<Task>(), TimeSpan.FromSeconds(30)).ShouldEqual(true);

            tasks.ForEach(t => t.Result.ShouldEqual(0));
        }
    }
}
