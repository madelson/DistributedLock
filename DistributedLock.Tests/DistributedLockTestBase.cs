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

        private static readonly List<Command> Commands = new List<Command>();

        private Command RunLockTaker(params string[] args)
        {
            var command = Command.Run("DistributedLockTaker", args);
            Commands.Add(command);
            return command;
        }

        [ClassCleanup]
        public static void CleanupCommands()
        {
            Commands.ForEach(c =>
            {
                if (!c.Task.IsCompleted)
                {
                    c.Kill();
                }
            });
        }

        internal abstract IDistributedLock CreateLock(string name);

        internal abstract string GetSafeLockName(string name);
    }
}
