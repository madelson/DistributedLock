using Medallion.Threading.Sql;
using Medallion.Threading.Tests.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests
{
    /// <summary>
    /// These cases test "self-deadlock", where a semaphore acquire cannot possibly succeed because the current connection owns
    /// all tickets. Since this can only happen when a connection/transaction is re-used, we require
    /// <see cref="IExternalConnectionOrTransactionTestingSqlConnectionManagementProvider"/> on our providers.
    /// </summary>
    [TestClass]
    public abstract class SqlDistributedSemaphoreSelfDeadlockTestCases<TConnectionManagementProvider> : TestBase
        where TConnectionManagementProvider : TestingSqlConnectionManagementProvider, IExternalConnectionOrTransactionTestingSqlConnectionManagementProvider, new()
    {
        [TestMethod]
        public void TestSelfDeadlockThrowsOnInfiniteWait()
        {
            using (var engine = this.CreateEngine())
            {
                var semaphore = engine.CreateSemaphore(nameof(TestSelfDeadlockThrowsOnInfiniteWait), maxCount: 2);
                semaphore.Acquire();
                semaphore.Acquire();
                var ex = TestHelper.AssertThrows<DeadlockException>(() => semaphore.Acquire());
                ex.Message.Contains("Deadlock").ShouldEqual(true, ex.Message);
            }
        }

        [TestMethod]
        public void TestMultipleConnectionsCannotTriggerSelfDeadlock()
        {
            using (var engine = this.CreateEngine())
            {
                var semaphore1 = engine.CreateSemaphore(nameof(TestMultipleConnectionsCannotTriggerSelfDeadlock), maxCount: 2);
                var semaphore2 = engine.CreateSemaphore(nameof(TestMultipleConnectionsCannotTriggerSelfDeadlock), maxCount: 2);
                semaphore1.Acquire();
                semaphore2.Acquire();

                var source = new CancellationTokenSource();
                var acquireTask = semaphore1.AcquireAsync(cancellationToken: source.Token).Task;
                acquireTask.Wait(TimeSpan.FromSeconds(.1)).ShouldEqual(false);
                source.Cancel();
                acquireTask.ContinueWith(t => { }).Wait(TimeSpan.FromSeconds(10)).ShouldEqual(true);
                acquireTask.Status.ShouldEqual(TaskStatus.Canceled);
            }
        }

        [TestMethod]
        public void TestSelfDeadlockWaitsOnSpecifiedTime()
        {
            using (var engine = this.CreateEngine())
            {
                var semaphore = engine.CreateSemaphore(nameof(TestSelfDeadlockWaitsOnSpecifiedTime), maxCount: 1);
                semaphore.Acquire();

                var acquireTask = Task.Run(() => semaphore.TryAcquire(TimeSpan.FromSeconds(.2)));
                acquireTask.Wait(TimeSpan.FromSeconds(.05)).ShouldEqual(false);
                acquireTask.Wait(TimeSpan.FromSeconds(.3)).ShouldEqual(true);
                acquireTask.Result.ShouldEqual(null);
            }
        }

        [TestMethod]
        public void TestSelfDeadlockWaitRespectsCancellation()
        {
            using (var engine = this.CreateEngine())
            {
                var semaphore = engine.CreateSemaphore(nameof(TestSelfDeadlockWaitsOnSpecifiedTime), maxCount: 1);
                semaphore.Acquire();

                var source = new CancellationTokenSource();
                var acquireTask = semaphore.AcquireAsync(TimeSpan.FromSeconds(20), source.Token).Task;
                acquireTask.Wait(TimeSpan.FromSeconds(.1)).ShouldEqual(false);
                source.Cancel();
                acquireTask.ContinueWith(t => { }).Wait(TimeSpan.FromSeconds(10)).ShouldEqual(true);
                acquireTask.Status.ShouldEqual(TaskStatus.Canceled);
            }
        }

        private TestingSqlDistributedSemaphoreEngine<TConnectionManagementProvider> CreateEngine() => new TestingSqlDistributedSemaphoreEngine<TConnectionManagementProvider>();
    }
}
