using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Sql
{
    public abstract class SqlDistributedReaderWriterLockTestCases<TConnectionManagementProvider> : TestBase
        where TConnectionManagementProvider : TestingSqlConnectionManagementProvider, new()
    {
        [TestMethod]
        public void TestMultipleReadersSingleWriter()
        {
            using (var engine = this.CreateEngine())
            {
                var @lock = engine.CreateReaderWriterLock(nameof(TestMultipleReadersSingleWriter));

                var readHandle1 = engine.CreateReaderWriterLock(nameof(TestMultipleReadersSingleWriter)).TryAcquireReadLockAsync().Result;
                Assert.IsNotNull(readHandle1, this.GetType().ToString());
                var readHandle2 = engine.CreateReaderWriterLock(nameof(TestMultipleReadersSingleWriter)).TryAcquireReadLockAsync().Result;
                Assert.IsNotNull(readHandle2, this.GetType().ToString());

                using (var handle = @lock.TryAcquireUpgradeableReadLock())
                {
                    Assert.IsNotNull(handle);

                    var readHandle3 = engine.CreateReaderWriterLock(nameof(TestMultipleReadersSingleWriter)).TryAcquireReadLock();
                    Assert.IsNotNull(readHandle3);

                    engine.CreateReaderWriterLock(nameof(TestMultipleReadersSingleWriter)).TryAcquireUpgradeableReadLock().ShouldEqual(null);
                    engine.CreateReaderWriterLock(nameof(TestMultipleReadersSingleWriter)).TryAcquireWriteLock().ShouldEqual(null);

                    readHandle3.Dispose();
                }

                readHandle1.Dispose();
                readHandle2.Dispose();

                using (var writeHandle = engine.CreateReaderWriterLock(nameof(TestMultipleReadersSingleWriter)).TryAcquireUpgradeableReadLock())
                {
                    Assert.IsNotNull(writeHandle);
                }
            }
        }

        [TestMethod]
        public void TestUpgradeToWriteLock()
        {
            using (var engine = this.CreateEngine())
            {
                var @lock = engine.CreateReaderWriterLock(nameof(TestUpgradeToWriteLock));

                var readHandle = engine.CreateReaderWriterLock(nameof(TestUpgradeToWriteLock)).AcquireReadLock();

                Task<IDisposable> readTask;
                using (var upgradeableHandle = @lock.AcquireUpgradeableReadLockAsync().Result)
                {
                    upgradeableHandle.TryUpgradeToWriteLock().ShouldEqual(false); // read lock still held

                    readHandle.Dispose();

                    upgradeableHandle.TryUpgradeToWriteLock().ShouldEqual(true);

                    readTask = engine.CreateReaderWriterLock(nameof(TestUpgradeToWriteLock)).AcquireReadLockAsync();
                    readTask.Wait(TimeSpan.FromSeconds(.1)).ShouldEqual(false, "write lock held");
                }

                readTask.Wait(TimeSpan.FromSeconds(10)).ShouldEqual(true, "write lock released");
                readTask.Result.Dispose();
            }
        }

        [TestMethod]
        public void TestReaderWriterLockBadArguments()
        {
            using (var engine = this.CreateEngine())
            {
                var @lock = engine.CreateReaderWriterLock(nameof(TestReaderWriterLockBadArguments));
                TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.AcquireReadLock(TimeSpan.FromSeconds(-2)));
                TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.AcquireReadLockAsync(TimeSpan.FromSeconds(-2)));
                TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.TryAcquireReadLock(TimeSpan.FromSeconds(-2)));
                TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.TryAcquireReadLockAsync(TimeSpan.FromSeconds(-2)));
                TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.AcquireReadLock(TimeSpan.FromSeconds(int.MaxValue)));
                TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.AcquireReadLockAsync(TimeSpan.FromSeconds(int.MaxValue)));
                TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.TryAcquireReadLock(TimeSpan.FromSeconds(int.MaxValue)));
                TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.TryAcquireReadLockAsync(TimeSpan.FromSeconds(int.MaxValue)));

                TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.AcquireUpgradeableReadLock(TimeSpan.FromSeconds(-2)));
                TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.AcquireUpgradeableReadLockAsync(TimeSpan.FromSeconds(-2)));
                TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.TryAcquireUpgradeableReadLock(TimeSpan.FromSeconds(-2)));
                TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.TryAcquireUpgradeableReadLockAsync(TimeSpan.FromSeconds(-2)));
                TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.AcquireUpgradeableReadLock(TimeSpan.FromSeconds(int.MaxValue)));
                TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.AcquireUpgradeableReadLockAsync(TimeSpan.FromSeconds(int.MaxValue)));
                TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.TryAcquireUpgradeableReadLock(TimeSpan.FromSeconds(int.MaxValue)));
                TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.TryAcquireUpgradeableReadLockAsync(TimeSpan.FromSeconds(int.MaxValue)));

                TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.AcquireWriteLock(TimeSpan.FromSeconds(-2)));
                TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.AcquireWriteLockAsync(TimeSpan.FromSeconds(-2)));
                TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.TryAcquireWriteLock(TimeSpan.FromSeconds(-2)));
                TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.TryAcquireWriteLockAsync(TimeSpan.FromSeconds(-2)));
                TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.AcquireWriteLock(TimeSpan.FromSeconds(int.MaxValue)));
                TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.AcquireWriteLockAsync(TimeSpan.FromSeconds(int.MaxValue)));
                TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.TryAcquireWriteLock(TimeSpan.FromSeconds(int.MaxValue)));
                TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => @lock.TryAcquireWriteLockAsync(TimeSpan.FromSeconds(int.MaxValue)));

                using (var upgradeableHandle = @lock.AcquireUpgradeableReadLock())
                {
                    TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => upgradeableHandle.UpgradeToWriteLock(TimeSpan.FromSeconds(-2)));
                    TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => upgradeableHandle.UpgradeToWriteLockAsync(TimeSpan.FromSeconds(-2)));
                    TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => upgradeableHandle.TryUpgradeToWriteLock(TimeSpan.FromSeconds(-2)));
                    TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => upgradeableHandle.TryUpgradeToWriteLockAsync(TimeSpan.FromSeconds(-2)));
                    TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => upgradeableHandle.UpgradeToWriteLock(TimeSpan.FromSeconds(int.MaxValue)));
                    TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => upgradeableHandle.UpgradeToWriteLockAsync(TimeSpan.FromSeconds(int.MaxValue)));
                    TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => upgradeableHandle.TryUpgradeToWriteLock(TimeSpan.FromSeconds(int.MaxValue)));
                    TestHelper.AssertThrows<ArgumentOutOfRangeException>(() => upgradeableHandle.TryUpgradeToWriteLockAsync(TimeSpan.FromSeconds(int.MaxValue)));
                }
            }
        }

        [TestMethod]
        public void TestUpgradeableHandleDisposal()
        {
            using (var engine = this.CreateEngine())
            {
                var @lock = engine.CreateReaderWriterLock(nameof(TestUpgradeableHandleDisposal));

                var handle = @lock.AcquireUpgradeableReadLock();
                handle.Dispose();
                TestHelper.AssertDoesNotThrow(() => handle.Dispose());
                TestHelper.AssertThrows<ObjectDisposedException>(() => handle.TryUpgradeToWriteLock());
                TestHelper.AssertThrows<ObjectDisposedException>(() => handle.TryUpgradeToWriteLockAsync());
                TestHelper.AssertThrows<ObjectDisposedException>(() => handle.UpgradeToWriteLock());
                TestHelper.AssertThrows<ObjectDisposedException>(() => handle.UpgradeToWriteLockAsync());
            }
        }

        [TestMethod]
        public void TestUpgradeableHandleMultipleUpgrades()
        {
            using (var engine = this.CreateEngine())
            {
                var @lock = engine.CreateReaderWriterLock(nameof(TestUpgradeableHandleMultipleUpgrades));

                using (var upgradeHandle = @lock.AcquireUpgradeableReadLock())
                {
                    upgradeHandle.UpgradeToWriteLock();
                    TestHelper.AssertThrows<InvalidOperationException>(() => upgradeHandle.TryUpgradeToWriteLock());
                }
            }
        }

        private TestingSqlDistributedReaderWriterLockEngine<TConnectionManagementProvider> CreateEngine() =>
            new TestingSqlDistributedReaderWriterLockEngine<TConnectionManagementProvider>();
    }
}
