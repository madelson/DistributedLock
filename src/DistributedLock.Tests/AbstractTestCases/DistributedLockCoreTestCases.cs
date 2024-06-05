using Medallion.Shell;
using Medallion.Threading.Internal;
using NUnit.Framework;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Medallion.Threading.Tests;

public abstract class DistributedLockCoreTestCases<TLockProvider, TStrategy>
    where TLockProvider : TestingLockProvider<TStrategy>, new()
    where TStrategy : TestingSynchronizationStrategy, new()
{
    private TLockProvider _lockProvider = default!;
    private readonly List<Action> _cleanupActions = [];

    [SetUp] 
    public async Task SetUp()
    {
        this._lockProvider = new TLockProvider();
        await this._lockProvider.SetupAsync();
    }

    [TearDown] 
    public async Task TearDown()
    {
        this._cleanupActions.ForEach(a => a());
        this._cleanupActions.Clear();
        await this._lockProvider.DisposeAsync();
    }

    [Test]
    public void BasicTest()
    {
        var @lock = this._lockProvider.CreateLock(nameof(BasicTest));
        var lock2 = this._lockProvider.CreateLock(nameof(BasicTest) + "2");

        using (var handle = @lock.TryAcquire())
        {
            Assert.That(handle, Is.Not.Null, this.GetType() + ": should be able to acquire new lock");

            using (var nestedHandle = @lock.TryAcquire())
            {
                Assert.That(nestedHandle, Is.Null, "should not be reentrant");
            }

            using var nestedHandle2 = lock2.TryAcquire();
            Assert.That(nestedHandle2, Is.Not.Null, this.GetType() + ": should be able to acquire a different lock");
        }

        using (var handle = @lock.TryAcquire())
        {
            Assert.That(handle, Is.Not.Null, this.GetType() + ": should be able to re-acquire after releasing");
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
            Assert.That(handle, Is.Not.Null, this.GetType().Name);

            using (var nestedHandle = await @lock.TryAcquireAsync())
            {
                Assert.That(nestedHandle, Is.Null, this.GetType().Name);
            }

            await using var nestedHandle2 = lock2.TryAcquireAsync().AsTask().Result;
            Assert.That(nestedHandle2, Is.Not.Null, this.GetType().Name);
        }

        await using (var handle = await @lock.TryAcquireAsync())
        {
            Assert.That(handle, Is.Not.Null, this.GetType().Name);
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
        Assert.That(handle, Is.Not.Null);
        handle.Dispose();
        var handle2 = @lock.Acquire(TimeSpan.FromSeconds(30));
        Assert.DoesNotThrow(handle.Dispose);
        Assert.DoesNotThrow(handle2.Dispose);
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
            Assert.That(syncAcquireTask.Exception?.InnerException, Is.InstanceOf<TimeoutException>(), "sync acquire");

            var asyncAcquireTask = @lock.AcquireAsync(timeout).AsTask();
            asyncAcquireTask.ContinueWith(_ => { }).Wait(waitTime).ShouldEqual(true, "async acquire");
            Assert.That(asyncAcquireTask.Exception!.InnerException, Is.InstanceOf<TimeoutException>(), "async acquire");

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
            // Task.Run() forces true asynchrony even for locks that don't support it
            var blocked = Task.Run(() => @lock.AcquireAsync(cancellationToken: source.Token).AsTask());
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
    public async Task TestParallelism()
    {
        var taskCount = 100;
        this._lockProvider.Strategy.PrepareForHighContention(ref taskCount);

        // NOTE: if this test fails for Postgres, we may need to raise the default connection limit. This can 
        // be done by setting max_connections in C:\Program Files\PostgreSQL\<version>\data\postgresql.conf or 
        // /var/lib/pgsql/<version>/data/postgresql.conf and then restarting Postgres. I set max_connections = 10000.
        // See https://docs.alfresco.com/5.0/tasks/postgresql-config.html

        var locks = Enumerable.Range(0, taskCount)
            .Select(_ => this._lockProvider.CreateLock("parallel_test"))
            .ToArray();
        var counter = 0;
        // Task.Run() ensures true parallelism even for locks that don't support it
        var tasks = Enumerable.Range(0, taskCount).Select(i => Task.Run(async () =>
            {
                await using (await locks[i].AcquireAsync())
                {
                    // increment going in
                    if (Interlocked.Increment(ref counter) == 2)
                    {
                        Assert.Fail($"Concurrent lock acquisitions ({this.GetType()}");
                    }

                    // hang out for a bit to ensure concurrency
                    await Task.Delay(TimeSpan.FromMilliseconds(10));

                    // decrement and return on the way out (returns # inside the lock when this left ... should be 0)
                    return Interlocked.Decrement(ref counter);
                }
            }))
            .ToList();

        var failure = new TaskCompletionSource<Exception>();
        foreach (var task in tasks)
        {
            _ = task.ContinueWith(t => failure.TrySetException(t.Exception!), TaskContinuationOptions.OnlyOnFaulted);
        }

        var timeout = Task.Delay(TimeSpan.FromSeconds(30));

        var completed = await Task.WhenAny(Task.WhenAll(tasks), failure.Task, timeout);
        Assert.That(completed, Is.Not.SameAs(failure.Task), $"Failed with {(failure.Task.IsFaulted ? failure.Task.Exception!.ToString() : null)}");
        Assert.That(completed, Is.Not.SameAs(timeout), $"Timed out! (only {tasks.Count(t => t.IsCompleted)}/{taskCount} completed)");

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

        Assert.That(this._lockProvider.GetSafeName(longName2), Is.Not.EqualTo(this._lockProvider.GetSafeName(longName1)));
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
        Assert.That(upperName, Is.Not.EqualTo(lowerName));
        if (StringComparer.OrdinalIgnoreCase.Equals(lowerName, upperName))
        {
            // if the names vary only by case, test that they are different locks
            await using (await this._lockProvider.CreateLockWithExactName(lowerName).AcquireAsync())
            await using (var handle = await this._lockProvider.CreateLockWithExactName(upperName).TryAcquireAsync())
            {
                Assert.That(handle, Is.Not.Null);
            }
        }
        else
        {
            // otherwise, check that the names still contain the suffixes we added
            Assert.That(lowerName.IndexOf(lowerBaseName, StringComparison.OrdinalIgnoreCase) >= 0, Is.True);
            Assert.That(upperName.IndexOf(upperBaseName, StringComparison.OrdinalIgnoreCase) >= 0, Is.True);
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

        var handle = await @lock.AcquireAsync();
        try
        {
            handle.HandleLostToken.CanBeCanceled.ShouldEqual(handleLostHelper != null);
            Assert.That(handle.HandleLostToken.IsCancellationRequested, Is.False);

            if (handleLostHelper != null)
            {
                using var canceledEvent = new ManualResetEventSlim(initialState: false);
                using var registration = handle.HandleLostToken.Register(canceledEvent.Set);

                Assert.That(canceledEvent.Wait(TimeSpan.FromSeconds(.05)), Is.False);

                handleLostHelper.Dispose();

                Assert.That(canceledEvent.Wait(TimeSpan.FromSeconds(10)), Is.True);
                Assert.That(handle.HandleLostToken.IsCancellationRequested, Is.True);
            }
        }
        finally
        {
            // when the handle is lost, Dispose() may throw
            try { await handle.DisposeAsync(); }
            catch { }
        }

        Assert.Throws<ObjectDisposedException>(() => handle.HandleLostToken.GetType());
    }

    [Test]
    public async Task TestHandleLostReturnsAlreadyCanceledIfHandleAlreadyLost()
    {
        // pre-create the lock so that semaphore5 tickets don't get created on the connection
        // we're going to kill
        this._lockProvider.CreateLock(nameof(TestHandleLostReturnsAlreadyCanceledIfHandleAlreadyLost));

        var handleLostHelper = this._lockProvider.Strategy.PrepareForHandleLost();
        if (handleLostHelper == null) { Assert.Pass(); }

        var @lock = this._lockProvider.CreateLock(nameof(TestHandleLostReturnsAlreadyCanceledIfHandleAlreadyLost));

        using var handle = await @lock.AcquireAsync();
        
        handleLostHelper!.Dispose();

        using var canceledEvent = new ManualResetEventSlim(initialState: false);
        handle.HandleLostToken.Register(canceledEvent.Set);
        Assert.That(canceledEvent.Wait(TimeSpan.FromSeconds(5)), Is.True);

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
        Assert.That(canceledEvent.Wait(TimeSpan.FromSeconds(.05)), Is.False);

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
        Assert.That(handle, Is.Not.Null, this.GetType().Name);
    }

    [Test]
    public void TestCrossProcess()
    {
        var lockName = this._lockProvider.GetUniqueSafeName();
        var command = this.RunLockTaker(this._lockProvider, this._lockProvider.GetCrossProcessLockType(), lockName, this._lockProvider.GetConnectionStringForCrossProcessTest());
        Assert.That(command.StandardOutput.ReadLineAsync().Wait(TimeSpan.FromSeconds(10)), Is.True);
        Assert.That(command.Task.Wait(TimeSpan.FromSeconds(.1)), Is.False);

        var @lock = this._lockProvider.CreateLockWithExactName(lockName);
        @lock.TryAcquire().ShouldEqual(null, this.GetType().Name);

        command.StandardInput.WriteLine("done");
        command.StandardInput.Flush();
        
        using var handle = @lock.TryAcquire(TimeSpan.FromSeconds(10));
        Assert.That(handle, Is.Not.Null, this.GetType().Name);

        Assert.That(command.Task.Wait(TimeSpan.FromSeconds(10)), Is.True);
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
        var command = this.RunLockTaker(this._lockProvider, this._lockProvider.GetCrossProcessLockType(), name, this._lockProvider.GetConnectionStringForCrossProcessTest());
        Assert.That(command.StandardOutput.ReadLineAsync().Wait(TimeSpan.FromSeconds(10)), Is.True);
        Assert.That(command.Task.IsCompleted, Is.False);

        var @lock = this._lockProvider.CreateLockWithExactName(name);

        var acquireTask = asyncWait
            // always use Task.Run() to force asynchrony even for locks that don't truly support it
            ? Task.Run(() => @lock.TryAcquireAsync(TimeSpan.FromSeconds(20)).AsTask())
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
        Assert.That(command.Task.ContinueWith(_ => { }).Wait(TimeSpan.FromSeconds(5)), Is.True, "lock taker should exit");

        if (this._lockProvider.SupportsCrossProcessAbandonment)
        {
            using var handle = acquireTask.Result;
            Assert.That(handle, Is.Not.Null, this.GetType().Name);
        }
        else
        {
            Assert.That(acquireTask.Wait(TimeSpan.FromSeconds(1)), Is.False);
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
