using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Data
{
    /// <summary>
    /// These cases test "self-deadlock", where a semaphore acquire cannot possibly succeed because the current connection owns
    /// all tickets. Since this can only happen when a connection/transaction is re-used, we require
    /// <see cref="TestingExternalConnectionOrTransactionSynchronizationStrategy{TDb}"/> on our providers.
    /// </summary>
    public abstract class SemaphoreSelfDeadlockTestCases<TSemaphoreProvider, TStrategy, TDb>
        where TSemaphoreProvider : TestingSemaphoreProvider<TStrategy>, new()
        where TStrategy : TestingExternalConnectionOrTransactionSynchronizationStrategy<TDb>, new()
        where TDb : ITestingDb, new()
    {
        private TSemaphoreProvider _semaphoreProvider = default!;

        [SetUp] public void SetUp() => this._semaphoreProvider = new TSemaphoreProvider();
        [TearDown] public void TearDown() => this._semaphoreProvider.Dispose();

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
    }
}
