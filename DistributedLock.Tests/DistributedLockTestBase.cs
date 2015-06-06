using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests
{
    public abstract class DistributedLockTestBase
    {
        [TestMethod]
        public void BasicTest()
        {
            var @lock = this.CreateLock("ralph");
            var lock2 = this.CreateLock("ralph2");

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
        public void BasicAsyncTest()
        {
            var @lock = this.CreateLock("ralph");
            var lock2 = this.CreateLock("ralph2");

            using (var handle = @lock.TryAcquireAsync().Result)
            {
                Assert.IsNotNull(handle);

                using (var nestedHandle = @lock.TryAcquireAsync().Result)
                {
                    Assert.IsNull(nestedHandle);
                }

                using (var nestedHandle2 = lock2.TryAcquireAsync().Result)
                {
                    Assert.IsNotNull(nestedHandle2);
                }
            }

            using (var handle = @lock.TryAcquireAsync().Result)
            {
                Assert.IsNotNull(handle);
            }
        }

        [TestMethod]
        public void TestBadArguments()
        {
            var @lock = this.CreateLock(Guid.NewGuid().ToString());
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
            var @lock = this.CreateLock("gerald");

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
        public void TestParallelism()
        {
            var counter = 0;
            var tasks = Enumerable.Range(1, 100).Select(async _ =>
                {
                    var @lock = this.CreateLock("parallel_test");
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

        internal abstract IDistributedLock CreateLock(string name);
    }
}
