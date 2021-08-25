using Medallion.Shell;
using Medallion.Threading.Internal;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests
{
    public abstract class DistributedLockCoreTestCases<TLockProvider, TStrategy>
        where TLockProvider : TestingLockProvider<TStrategy>, new()
        where TStrategy : TestingSynchronizationStrategy, new()
    {
        private TLockProvider _lockProvider = default!;
        private readonly List<Action> _cleanupActions = new List<Action>();

        [SetUp] public void SetUp() => this._lockProvider = new TLockProvider();

        [TearDown] 
        public void TearDown()
        {
            this._cleanupActions.ForEach(a => a());
            this._cleanupActions.Clear();
            this._lockProvider.Dispose();
        }

        [Test]
        public void BasicTest()
        {
            var @lock = this._lockProvider.CreateLock(nameof(BasicTest));
            var lock2 = this._lockProvider.CreateLock(nameof(BasicTest) + "2");

            using (var handle = @lock.TryAcquire())
            {
                Assert.IsNotNull(handle, this.GetType() + ": should be able to acquire new lock");

                using (var nestedHandle = @lock.TryAcquire())
                {
                    Assert.IsNull(nestedHandle, "should not be reentrant");
                }

                using var nestedHandle2 = lock2.TryAcquire();
                Assert.IsNotNull(nestedHandle2, this.GetType() + ": should be able to acquire a different lock");
            }

            using (var handle = @lock.TryAcquire())
            {
                Assert.IsNotNull(handle, this.GetType() + ": should be able to re-acquire after releasing");
            }
        }

        [Test]
        public async Task BasicAsyncTest()
        {
            // note: we intentionally have a mix of await using vs using and await 
            // vs .Result here to excercise various code paths

            var @lock = this._lockProvider.CreateLock(nameof(BasicAsyncTest));
            var lock2 = this._lockProvider.CreateLock(nameof(BasicAsyncTest) + "2");

            await using (var handle = await @lock.TryAcquireAsync())
            {
                Assert.IsNotNull(handle, this.GetType().Name);

                using (var nestedHandle = await @lock.TryAcquireAsync())
                {
                    Assert.IsNull(nestedHandle, this.GetType().Name);
                }

                await using var nestedHandle2 = lock2.TryAcquireAsync().AsTask().Result;
                Assert.IsNotNull(nestedHandle2, this.GetType().Name);
            }

            await using (var handle = await @lock.TryAcquireAsync())
            {
                Assert.IsNotNull(handle, this.GetType().Name);
            }
        }

        [Test]
        public void TestBadArguments()
        {
            var @lock = this._lockProvider.CreateLock(nameof(TestBadArguments));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.Acquire(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireAsync(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquire(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireAsync(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.Acquire(TimeSpan.FromSeconds(int.MaxValue)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireAsync(TimeSpan.FromSeconds(int.MaxValue)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquire(TimeSpan.FromSeconds(int.MaxValue)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireAsync(TimeSpan.FromSeconds(int.MaxValue)));
        }

        [Test]
        public void TestDisposeHandleIsIdempotent()
        {
            var @lock = this._lockProvider.CreateLock(nameof(TestDisposeHandleIsIdempotent));
            var handle = @lock.Acquire(TimeSpan.FromSeconds(30));
            Assert.IsNotNull(handle);
            handle.Dispose();
            var handle2 = @lock.Acquire(TimeSpan.FromSeconds(30));
            Assert.DoesNotThrow(() => handle.Dispose());
            Assert.DoesNotThrow(() => handle2.Dispose());
        }

        [Test]
        [NonParallelizable, Retry(tryCount: 3)] // timing-sensitive
        public void TestTimeouts()
        {
            // use a randomized name in case we end up retrying
            var lockName = Guid.NewGuid().ToString();

            var @lock = this._lockProvider.CreateLock(lockName);
            // acquire with a different lock instance to avoid reentrancy mattering
            using (this._lockProvider.CreateLock(lockName).Acquire())
            {
                var timeout = TimeSpan.FromSeconds(.2);
                var waitTime = TimeSpan.FromSeconds(.5);

                var syncAcquireTask = Task.Run(() => @lock.Acquire(timeout));
                syncAcquireTask.ContinueWith(_ => { }).Wait(waitTime).ShouldEqual(true, "sync acquire");
                Assert.IsInstanceOf<TimeoutException>(syncAcquireTask.Exception?.InnerException, "sync acquire");

                var asyncAcquireTask = @lock.AcquireAsync(timeout).AsTask();
                asyncAcquireTask.ContinueWith(_ => { }).Wait(waitTime).ShouldEqual(true, "async acquire");
                Assert.IsInstanceOf<TimeoutException>(asyncAcquireTask.Exception!.InnerException, "async acquire");

                var syncTryAcquireTask = Task.Run(() => @lock.TryAcquire(timeout));
                syncTryAcquireTask.Wait(waitTime).ShouldEqual(true, "sync tryAcquire");
                syncTryAcquireTask.Result.ShouldEqual(null, "sync tryAcquire");

                var asyncTryAcquireTask = @lock.TryAcquireAsync(timeout).AsTask();
                asyncTryAcquireTask.Wait(waitTime).ShouldEqual(true, "async tryAcquire");
                asyncTryAcquireTask.Result.ShouldEqual(null, "async tryAcquire");
            }
        }

        [Test]
        public void CancellationTest()
        {
            var lockName = nameof(CancellationTest);
            var @lock = this._lockProvider.CreateLock(lockName);

            var source = new CancellationTokenSource();
            using (var handle = this._lockProvider.CreateLock(lockName).Acquire())
            {
                var blocked = @lock.AcquireAsync(cancellationToken: source.Token).AsTask();
                blocked.Wait(TimeSpan.FromSeconds(.1)).ShouldEqual(false);
                source.Cancel();
                blocked.ContinueWith(_ => { }).Wait(TimeSpan.FromSeconds(10)).ShouldEqual(true, this.GetType().Name);
                blocked.Status.ShouldEqual(TaskStatus.Canceled, (blocked.Exception ?? (object)"no exception").ToString());
            }

            // already canceled
            source = new CancellationTokenSource();
            source.Cancel();
            Assert.Catch<OperationCanceledException>(() => @lock.Acquire(cancellationToken: source.Token));
        }

        [Test]
        public void TestParallelism()
        {
            this._lockProvider.Strategy.PrepareForHighContention();

            // NOTE: if this test fails for Postgres, we may need to raise the default connection limit. This can 
            // be done by setting max_connections in C:\Program Files\PostgreSQL\<version>\data\postgresql.conf or 
            // /var/lib/pgsql/<version>/data/postgresql.conf and then restarting Postgres.
            // See https://docs.alfresco.com/5.0/tasks/postgresql-config.html

            var counter = 0;
            var tasks = Enumerable.Range(1, 100).Select(async _ =>
                {
                    var @lock = this._lockProvider.CreateLock("parallel_test");
                    await using (await @lock.AcquireAsync())
                    {
                        // increment going in
                        if (Interlocked.Increment(ref counter) == 2)
                        {
                            Assert.Fail("Concurrent lock acquisitions");
                        }

                        // hang out for a bit to ensure concurrency
                        await Task.Delay(TimeSpan.FromMilliseconds(10));

                        // decrement and return on the way out (returns # inside the lock when this left ... should be 0)
                        return Interlocked.Decrement(ref counter);
                    }
                })
                .ToList();

            Task.WaitAll(tasks.ToArray<Task>(), TimeSpan.FromSeconds(30)).ShouldEqual(true, this.GetType().Name);

            tasks.ForEach(t => t.Result.ShouldEqual(0));
        }

        [Test]
        [NonParallelizable] // takes locks with known names
        public void TestGetSafeName()
        {
            Assert.Catch<ArgumentNullException>(() => this._lockProvider.GetSafeName(null!));

            foreach (var name in new[] { string.Empty, new string('a', 1000), @"\\\\\", new string('\\', 1000) })
            {
                var safeName = this._lockProvider.GetSafeName(name);
                Assert.DoesNotThrow(() => this._lockProvider.CreateLockWithExactName(safeName).Acquire(TimeSpan.FromSeconds(10)).Dispose(), $"{this.GetType().Name}: could not acquire '{name}'");
            }
        }

        [Test]
        public void TestGetSafeLockNameIsCaseSensitive()
        {
            var longName1 = new string('a', 1000);
            var longName2 = new string('a', longName1.Length - 1) + "A";
            StringComparer.OrdinalIgnoreCase.Equals(longName1, longName2).ShouldEqual(true, "sanity check");

            Assert.AreNotEqual(this._lockProvider.GetSafeName(longName1), this._lockProvider.GetSafeName(longName2));
        }

        [Test]
        public async Task TestLockNamesAreCaseSensitive()
        {
            // the goal here is to construct 2 valid lock names that differ only by case. We start by generating a hash name
            // that is unique to this test yet stable across runs. Then we truncate it to avoid need for further hashing in Postgres
            // (which only supports very short ASCII string names). Finally, we re-run through GetSafeName to pick up any special prefix
            // that is needed (e. g. for wait handles)
            using var sha1 = SHA1.Create();
            var uniqueHashName = BitConverter.ToString(sha1.ComputeHash(Encoding.UTF8.GetBytes(this._lockProvider.GetUniqueSafeName())))
                .Replace("-", string.Empty)
                // normalize to upper case per https://docs.microsoft.com/en-us/visualstudio/code-quality/ca1308?view=vs-2019
                .ToUpperInvariant();
            var lowerBaseName = $"{uniqueHashName.Substring(0, 6)}_a";
            var lowerName = this._lockProvider.GetSafeName(lowerBaseName);
            var upperBaseName = $"{uniqueHashName.Substring(0, 6)}_A";
            var upperName = this._lockProvider.GetSafeName(upperBaseName);
            // make sure we succeeded in generating what we set out to generate
            Assert.AreNotEqual(lowerName, upperName);
            if (StringComparer.OrdinalIgnoreCase.Equals(lowerName, upperName))
            {
                // if the names vary only by case, test that they are different locks
                await using (await this._lockProvider.CreateLockWithExactName(lowerName).AcquireAsync())
                await using (var handle = await this._lockProvider.CreateLockWithExactName(upperName).TryAcquireAsync())
                {
                    Assert.IsNotNull(handle);
                }
            }
            else
            {
                // otherwise, check that the names still contain the suffixes we added
                Assert.IsTrue(lowerName.IndexOf(lowerBaseName, StringComparison.OrdinalIgnoreCase) >= 0);
                Assert.IsTrue(upperName.IndexOf(upperBaseName, StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        [Test]
        public void TestCanceledAlreadyThrowsForSyncAndDoesNotThrowForAsync()
        {
            using var source = new CancellationTokenSource();
            source.Cancel();

            var @lock = this._lockProvider.CreateLock("already-canceled");

            Assert.Catch<OperationCanceledException>(() => @lock.Acquire(cancellationToken: source.Token));
            Assert.Catch<OperationCanceledException>(() => @lock.TryAcquire(cancellationToken: source.Token));

            var acquireTask = @lock.AcquireAsync(cancellationToken: source.Token).AsTask();
            acquireTask.ContinueWith(_ => { }).Wait(TimeSpan.FromSeconds(10)).ShouldEqual(true);
            acquireTask.IsCanceled.ShouldEqual(true, "acquire");

            var tryAcquireTask = @lock.TryAcquireAsync(cancellationToken: source.Token).AsTask();
            tryAcquireTask.ContinueWith(_ => { }).Wait(TimeSpan.FromSeconds(10)).ShouldEqual(true);
            tryAcquireTask.IsCanceled.ShouldEqual(true, "tryAcquire");
        }

        [Test]
        public async Task TestHandleLostTriggersCorrectly()
        {
            // pre-create the lock so that semaphore5 tickets don't get created on the connection
            // we're going to kill
            this._lockProvider.CreateLock(nameof(TestHandleLostTriggersCorrectly));

            var handleLostHelper = this._lockProvider.Strategy.PrepareForHandleLost();

            var @lock = this._lockProvider.CreateLock(nameof(TestHandleLostTriggersCorrectly));

            using var handle = await @lock.AcquireAsync();

            handle.HandleLostToken.CanBeCanceled.ShouldEqual(handleLostHelper != null);
            Assert.IsFalse(handle.HandleLostToken.IsCancellationRequested);

            if (handleLostHelper != null)
            {
                using var canceledEvent = new ManualResetEventSlim(initialState: false);
                using var registration = handle.HandleLostToken.Register(canceledEvent.Set);

                Assert.IsFalse(canceledEvent.Wait(TimeSpan.FromSeconds(.05)));

                handleLostHelper.Dispose();

                Assert.IsTrue(canceledEvent.Wait(TimeSpan.FromSeconds(10)));
                Assert.IsTrue(handle.HandleLostToken.IsCancellationRequested);
            }

            // when the handle is lost, Dispose() may throw
            try { await handle.DisposeAsync(); }
            catch { }

            Assert.Throws<ObjectDisposedException>(() => handle.HandleLostToken.GetType());
        }

        [Test]
        public async Task TestHandleLostReturnsAlreadyCanceledIfHandleAlreadyLost()
        {
            // pre-create the lock so that semaphore5 tickets don't get created on the connection
            // we're going to kill
            this._lockProvider.CreateLock(nameof(TestHandleLostReturnsAlreadyCanceledIfHandleAlreadyLost));

            var handleLostHelper = this._lockProvider.Strategy.PrepareForHandleLost();
            if (handleLostHelper == null)
            {
                Assert.Pass();
            }

            var @lock = this._lockProvider.CreateLock(nameof(TestHandleLostReturnsAlreadyCanceledIfHandleAlreadyLost));

            using var handle = await @lock.AcquireAsync();

            handleLostHelper!.Dispose();

            using var canceledEvent = new ManualResetEventSlim(initialState: false);
            handle.HandleLostToken.Register(canceledEvent.Set);
            Assert.IsTrue(canceledEvent.Wait(TimeSpan.FromSeconds(5)));

            // when the handle is lost, Dispose() may throw
            try { handle.Dispose(); }
            catch { }
        }

        [Test]
        public void TestCanSafelyDisposeWhileMonitoring()
        {
            var @lock = this._lockProvider.CreateLock(nameof(TestCanSafelyDisposeWhileMonitoring));

            using var handle = @lock.Acquire();

            // force monitoring to happen
            using var canceledEvent = new ManualResetEventSlim(initialState: false);
            using var registration = handle.HandleLostToken.Register(canceledEvent.Set);
            Assert.IsFalse(canceledEvent.Wait(TimeSpan.FromSeconds(.05)));

            Assert.DoesNotThrow(handle.Dispose);
        }

        [Test]
        public async Task TestLockAbandonment()
        {
            const string LockName = nameof(TestLockAbandonment);

            // pre-create the lock so that the semaphore5 provider will allocate the extra tickets
            // against a connection that won't get cleand up when we force additional cleanup
            this._lockProvider.CreateLock(LockName);

            this._lockProvider.Strategy.PrepareForHandleAbandonment();
            new Action<string>(name => this._lockProvider.CreateLock(name).Acquire())(LockName);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await ManagedFinalizerQueue.Instance.FinalizeAsync();
            this._lockProvider.Strategy.PerformAdditionalCleanupForHandleAbandonment();

            using var handle = this._lockProvider.CreateLock(LockName).TryAcquire();
            Assert.IsNotNull(handle, this.GetType().Name);
        }

        [Test]
        public void TestCrossProcess()
        {
            var lockName = this._lockProvider.GetUniqueSafeName();
            var command = this.RunLockTaker(this._lockProvider, this._lockProvider.GetCrossProcessLockType(), lockName);
            Assert.IsTrue(command.StandardOutput.ReadLineAsync().Wait(TimeSpan.FromSeconds(10)));
            Assert.IsFalse(command.Task.Wait(TimeSpan.FromSeconds(.1)));

            var @lock = this._lockProvider.CreateLockWithExactName(lockName);
            @lock.TryAcquire().ShouldEqual(null, this.GetType().Name);

            command.StandardInput.WriteLine("done");
            command.StandardInput.Flush();
            
            using var handle = @lock.TryAcquire(TimeSpan.FromSeconds(10));
            Assert.IsNotNull(handle, this.GetType().Name);

            Assert.IsTrue(command.Task.Wait(TimeSpan.FromSeconds(10)));
        }

        [Test]
        public void TestCrossProcessAbandonment()
        {
            this.CrossProcessAbandonmentHelper(asyncWait: false, kill: false);
        }

        [Test]
        public void TestCrossProcessAbandonmentWithKill()
        {
            this.CrossProcessAbandonmentHelper(asyncWait: true, kill: true);
        }

        private void CrossProcessAbandonmentHelper(bool asyncWait, bool kill)
        {
            var name = this._lockProvider.GetUniqueSafeName($"cpl-{asyncWait}-{kill}");
            var command = this.RunLockTaker(this._lockProvider, this._lockProvider.GetCrossProcessLockType(), name);
            Assert.IsTrue(command.StandardOutput.ReadLineAsync().Wait(TimeSpan.FromSeconds(10)));
            Assert.IsFalse(command.Task.IsCompleted);

            var @lock = this._lockProvider.CreateLockWithExactName(name);

            var acquireTask = asyncWait
                ? @lock.TryAcquireAsync(TimeSpan.FromSeconds(20)).AsTask()
                : Task.Run(() => @lock.TryAcquire(TimeSpan.FromSeconds(20)));
            acquireTask.Wait(TimeSpan.FromSeconds(.1)).ShouldEqual(false, this.GetType().Name);

            if (kill)
            {
                command.Kill();
            }
            else
            {
                command.StandardInput.WriteLine("abandon");
                command.StandardInput.Flush();
            }
            // make sure it actually exits
            Assert.IsTrue(command.Task.ContinueWith(_ => { }).Wait(TimeSpan.FromSeconds(5)), "lock taker should exit");

            if (this._lockProvider.SupportsCrossProcessAbandonment)
            {
                using var handle = acquireTask.Result;
                Assert.IsNotNull(handle, this.GetType().Name);
            }
            else
            {
                Assert.IsFalse(acquireTask.Wait(TimeSpan.FromSeconds(1)));
            }
        }

        private Command RunLockTaker(TLockProvider engine, params string[] args)
        {
            const string Configuration =
#if DEBUG
                "Debug";
#else
                "Release";
#endif
            var exeExtension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty;
            var exePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "DistributedLockTaker", "bin", Configuration, TargetFramework.Current, "DistributedLockTaker" + exeExtension);

            var command = Command.Run(exePath, args, o => o.WorkingDirectory(TestContext.CurrentContext.TestDirectory).ThrowOnError(true))
                .RedirectStandardErrorTo(Console.Error);
            this._cleanupActions.Add(() =>
            {
                if (!command.Task.IsCompleted)
                {
                    command.Kill();
                }
            });
            return command;
        }
    }
}
