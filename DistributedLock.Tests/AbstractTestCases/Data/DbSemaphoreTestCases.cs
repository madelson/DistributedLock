using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Data;

public abstract class DbSemaphoreTestCases<TSemaphoreProvider, TStrategy, TDb>
    where TSemaphoreProvider : TestingSemaphoreProvider<TStrategy>, new()
    where TStrategy : TestingExternalConnectionOrTransactionSynchronizationStrategy<TDb>, new()
    where TDb : TestingDb, new()
{
    private TSemaphoreProvider _semaphoreProvider = default!;

    [SetUp] public void SetUp() => this._semaphoreProvider = new TSemaphoreProvider();
    [TearDown] public void TearDown() => this._semaphoreProvider.Dispose();

    /// <summary>
    /// This case and several that follow test "self-deadlock", where a semaphore acquire cannot possibly succeed because 
    /// the current connection owns all tickets. Since this can only happen when a connection/transaction is re-used, we require
    /// <see cref="TestingExternalConnectionOrTransactionSynchronizationStrategy{TDb}"/> on our providers.
    /// </summary>
    [Test]
    public void TestSelfDeadlockThrowsOnInfiniteWait()
    {
        var semaphore = this._semaphoreProvider.CreateSemaphore(nameof(TestSelfDeadlockThrowsOnInfiniteWait), maxCount: 2);
        semaphore.Acquire();
        semaphore.Acquire();
        var ex = Assert.Catch<DeadlockException>(() => semaphore.Acquire());
        ex.Message.Contains("Deadlock").ShouldEqual(true, ex.Message);
    }

    [Test]
    public void TestMultipleConnectionsCannotTriggerSelfDeadlock()
    {
        var semaphore1 = this._semaphoreProvider.CreateSemaphore(nameof(TestMultipleConnectionsCannotTriggerSelfDeadlock), maxCount: 2);
        var semaphore2 = this._semaphoreProvider.CreateSemaphore(nameof(TestMultipleConnectionsCannotTriggerSelfDeadlock), maxCount: 2);
        semaphore1.Acquire();
        semaphore2.Acquire();

        var source = new CancellationTokenSource();
        var acquireTask = semaphore1.AcquireAsync(cancellationToken: source.Token).AsTask();
        acquireTask.Wait(TimeSpan.FromSeconds(.1)).ShouldEqual(false);
        source.Cancel();
        acquireTask.ContinueWith(t => { }).Wait(TimeSpan.FromSeconds(10)).ShouldEqual(true);
        acquireTask.Status.ShouldEqual(TaskStatus.Canceled);
    }

    [Test]
    public void TestSelfDeadlockWaitsOnSpecifiedTime()
    {
        var semaphore = this._semaphoreProvider.CreateSemaphore(nameof(TestSelfDeadlockWaitsOnSpecifiedTime), maxCount: 1);
        semaphore.Acquire();

        var acquireTask = Task.Run(() => semaphore.TryAcquire(TimeSpan.FromSeconds(.2)));
        acquireTask.Wait(TimeSpan.FromSeconds(.05)).ShouldEqual(false);
        acquireTask.Wait(TimeSpan.FromSeconds(.3)).ShouldEqual(true);
        acquireTask.Result.ShouldEqual(null);
    }

    [Test]
    public void TestSelfDeadlockWaitRespectsCancellation()
    {
        var semaphore = this._semaphoreProvider.CreateSemaphore(nameof(TestSelfDeadlockWaitsOnSpecifiedTime), maxCount: 1);
        semaphore.Acquire();

        var source = new CancellationTokenSource();
        var acquireTask = semaphore.AcquireAsync(TimeSpan.FromSeconds(20), source.Token).AsTask();
        acquireTask.Wait(TimeSpan.FromSeconds(.1)).ShouldEqual(false);
        source.Cancel();
        acquireTask.ContinueWith(t => { }).Wait(TimeSpan.FromSeconds(10)).ShouldEqual(true);
        acquireTask.Status.ShouldEqual(TaskStatus.Canceled);
    }

    [Test]
    public void TestSameNameDifferentCounts()
    {
        var longTimeout = TimeSpan.FromSeconds(5);

        // if 2 semaphores have different views of what the max count is, things still kind of
        // work. The semaphore with the higher count behaves normally. The semaphore with the lower
        // count behaves normally when the number of contenders is below it's count. After that, it
        // behaves unpredictably. For example, if we have counts 2 and 3 and the 3-semaphore holds 2 tickets,
        // then the 2-semaphore might or might not be able to acquire a ticket depending on whether the
        // 3-semaphore holds tickets 1&2 (no), 1&3 (yes), or 2&3 (yes). This test serves to document
        // the behavior that is more well-defined

        var semaphore2 = this._semaphoreProvider.CreateSemaphore(nameof(TestSameNameDifferentCounts), 2);
        var semaphore3 = this._semaphoreProvider.CreateSemaphore(nameof(TestSameNameDifferentCounts), 3);

        var handle1 = semaphore2.Acquire(longTimeout);
        var handle2 = semaphore3.Acquire(longTimeout);
        var handle3 = semaphore3.Acquire(longTimeout);
        semaphore2.TryAcquire().ShouldEqual(null);
        semaphore3.TryAcquire().ShouldEqual(null);

        handle1.Dispose();
        handle1 = semaphore3.Acquire(longTimeout);

        handle1.Dispose();
        handle2.Dispose();
        handle3.Dispose();
    }
}
