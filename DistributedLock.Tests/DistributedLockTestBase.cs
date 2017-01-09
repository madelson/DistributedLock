using Medallion.Shell;
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
                    (nestedHandle == null).ShouldEqual(!this.IsReentrant);
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
                    (nestedHandle == null).ShouldEqual(!this.IsReentrant);
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
        public void TestTimeouts()
        {
            var @lock = this.CreateLock("timeout");
            // acquire with a different lock instance to avoid reentrancy mattering
            using (this.CreateLock("timeout").Acquire())
            {
                var syncAcquireTask = Task.Run(() => @lock.Acquire(TimeSpan.FromSeconds(.1)));
                syncAcquireTask.ContinueWith(_ => { }).Wait(TimeSpan.FromSeconds(.2)).ShouldEqual(true, "sync acquire");
                Assert.IsInstanceOfType(syncAcquireTask.Exception.InnerException, typeof(TimeoutException), "sync acquire");

                var asyncAcquireTask = @lock.AcquireAsync(TimeSpan.FromSeconds(.1));
                asyncAcquireTask.ContinueWith(_ => { }).Wait(TimeSpan.FromSeconds(.2)).ShouldEqual(true, "async acquire");
                Assert.IsInstanceOfType(asyncAcquireTask.Exception.InnerException, typeof(TimeoutException), "async acquire");

                var syncTryAcquireTask = Task.Run(() => @lock.TryAcquire(TimeSpan.FromSeconds(.1)));
                syncTryAcquireTask.Wait(TimeSpan.FromSeconds(.2)).ShouldEqual(true, "sync tryAcquire");
                syncTryAcquireTask.Result.ShouldEqual(null, "sync tryAcquire");

                var asyncTryAcquireTask = @lock.TryAcquireAsync(TimeSpan.FromSeconds(.1));
                asyncTryAcquireTask.Wait(TimeSpan.FromSeconds(.2)).ShouldEqual(true, "async tryAcquire");
                asyncTryAcquireTask.Result.ShouldEqual(null, "async tryAcquire");
            }
        }

        [TestMethod]
        public void CancellationTest()
        {
            var @lock = this.CreateLock("gerald");

            var source = new CancellationTokenSource();
            using (var handle = this.CreateLock("gerald").Acquire())
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

        [TestMethod]
        public void TestGetSafeLockName()
        {
            TestHelper.AssertThrows<ArgumentNullException>(() => this.GetSafeLockName(null));

            foreach (var name in new[] { string.Empty, new string('a', 1000), @"\\\\\", new string('\\', 1000) })
            {
                var safeName = this.GetSafeLockName(name);
                TestHelper.AssertDoesNotThrow(() => this.CreateLock(safeName).Acquire().Dispose());
            }
        }

        [TestMethod]
        public void TestCanceledAlreadyThrowsForSyncAndDoesNotThrowForAsync()
        {
            using (var source = new CancellationTokenSource())
            {
                source.Cancel();

                var @lock = this.CreateLock("already-canceled");

                TestHelper.AssertThrows<OperationCanceledException>(() => @lock.Acquire(cancellationToken: source.Token));
                TestHelper.AssertThrows<OperationCanceledException>(() => @lock.TryAcquire(cancellationToken: source.Token));

                var acquireTask = @lock.AcquireAsync(cancellationToken: source.Token);
                acquireTask.ContinueWith(_ => { }).Wait(TimeSpan.FromSeconds(10)).ShouldEqual(true);
                acquireTask.IsCanceled.ShouldEqual(true, "acquire");

                var tryAcquireTask = @lock.TryAcquireAsync(cancellationToken: source.Token);
                tryAcquireTask.ContinueWith(_ => { }).Wait(TimeSpan.FromSeconds(10)).ShouldEqual(true);
                tryAcquireTask.IsCanceled.ShouldEqual(true, "tryAcquire");
            }
        }

        [TestMethod]
        public void TestLockAbandonment()
        {
            if (!this.SupportsInProcessAbandonment) { return; }

            var lockName = this.GetSafeLockName("abandoned_" + this.GetType().Name);
            new Action<string>(name => this.CreateLock(name).Acquire())(lockName);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            System.Data.SqlClient.SqlConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            using (var handle = this.CreateLock(lockName).TryAcquire())
            {
                Assert.IsNotNull(handle, this.GetType().Name);
            }
        }

        [TestMethod]
        public void TestCrossProcess()
        {
            var type = this.CreateLock("a").GetType().Name.Replace("DistributedLock", string.Empty).ToLowerInvariant();

            var command = this.RunLockTaker(type, "cpl");
            command.Task.Wait(TimeSpan.FromSeconds(.5)).ShouldEqual(false);

            var @lock = this.CreateLock("cpl");
            @lock.TryAcquire().ShouldEqual(null);

            command.StandardInput.WriteLine("done");
            command.StandardInput.Flush();

            using (var handle = @lock.TryAcquire(TimeSpan.FromSeconds(10)))
            {
                Assert.IsNotNull(handle);
            }
        }

        [TestMethod]
        public void TestCrossProcessAbandonment()
        {
            this.CrossProcessAbandonmentHelper(asyncWait: false, kill: false);
        }

        [TestMethod]
        public void TestCrossProcessAbandonmentAsync()
        {
            this.CrossProcessAbandonmentHelper(asyncWait: true, kill: false);
        }

        [TestMethod]
        public void TestCrossProcessAbandonmentWithKill()
        {
            this.CrossProcessAbandonmentHelper(asyncWait: false, kill: true);
        }

        [TestMethod]
        public void TestCrossProcessAbandonmentWithKillAsync()
        {
            this.CrossProcessAbandonmentHelper(asyncWait: true, kill: true);
        }
  
        private void CrossProcessAbandonmentHelper(bool asyncWait, bool kill) 
        {
            var type = this.CreateLock("a").GetType().Name.Replace("DistributedLock", string.Empty).ToLowerInvariant();

            var name = "cpl-" + asyncWait + "-" + kill;
            var command = this.RunLockTaker(type, name);
            command.Task.Wait(TimeSpan.FromSeconds(.5)).ShouldEqual(false);

            var @lock = this.CreateLock(name);

            var acquireTask = asyncWait
                ? @lock.TryAcquireAsync(TimeSpan.FromSeconds(10))
                : Task.Run(() => @lock.TryAcquire(TimeSpan.FromSeconds(10)));
            acquireTask.Wait(TimeSpan.FromSeconds(.1)).ShouldEqual(false);

            if (kill)
            {
                command.Kill();
            }
            else
            {
                command.StandardInput.WriteLine("abandon");
                command.StandardInput.Flush();
            }

            using (var handle = acquireTask.Result)
            {
                Assert.IsNotNull(handle);
            }
        }

        private Command RunLockTaker(params string[] args)
        {
            var command = Command.Run("DistributedLockTaker", args);
            this.AddCleanupAction(() => 
            {
                if (!command.Task.IsCompleted)
                {
                    command.Kill();
                }
            });
            return command;
        }

        private static readonly Queue<Action> CleanupActions = new Queue<Action>();

        protected void AddCleanupAction(Action action) 
        {
            lock (CleanupActions) 
            {
                CleanupActions.Enqueue(action);
            }
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            lock (CleanupActions) 
            {
                while (CleanupActions.Count > 0) 
                {
                    CleanupActions.Dequeue()();
                }
            }
        }

        internal virtual bool IsReentrant => false;
        internal virtual bool SupportsInProcessAbandonment => true;

        internal abstract IDistributedLock CreateLock(string name);

        internal abstract string GetSafeLockName(string name);
    }
}
