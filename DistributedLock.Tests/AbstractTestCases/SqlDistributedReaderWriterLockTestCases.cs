using Medallion.Threading.SqlServer;
using Medallion.Threading.Tests.Data;
using Medallion.Threading.Tests.SqlServer;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests
{
    public abstract class SqlDistributedReaderWriterLockTestCases<TConnectionManagementProvider>
        where TConnectionManagementProvider : TestingSqlConnectionManagementProvider, new()
    {
        [Test]
        public void TestMultipleReadersSingleWriter()
        {
            using var engine = this.CreateEngine();
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

                readHandle3!.Dispose();
            }

            readHandle1!.Dispose();
            readHandle2!.Dispose();

            using var writeHandle = engine.CreateReaderWriterLock(nameof(TestMultipleReadersSingleWriter)).TryAcquireUpgradeableReadLock();
            Assert.IsNotNull(writeHandle);
        }

        [Test]
        public void TestUpgradeToWriteLock()
        {
            using var engine = this.CreateEngine();
            var @lock = engine.CreateReaderWriterLock(nameof(TestUpgradeToWriteLock));

            var readHandle = engine.CreateReaderWriterLock(nameof(TestUpgradeToWriteLock)).AcquireReadLock();

            Task<SqlDistributedReaderWriterLockHandle> readTask;
            using (var upgradeableHandle = @lock.AcquireUpgradeableReadLockAsync().Result)
            {
                upgradeableHandle.TryUpgradeToWriteLock().ShouldEqual(false); // read lock still held

                readHandle.Dispose();

                upgradeableHandle.TryUpgradeToWriteLock().ShouldEqual(true);

                readTask = engine.CreateReaderWriterLock(nameof(TestUpgradeToWriteLock)).AcquireReadLockAsync().AsTask();
                readTask.Wait(TimeSpan.FromSeconds(.1)).ShouldEqual(false, "write lock held");
            }

            readTask.Wait(TimeSpan.FromSeconds(10)).ShouldEqual(true, "write lock released");
            readTask.Result.Dispose();
        }

        [Test]
        public void TestReaderWriterLockBadArguments()
        {
            using var engine = this.CreateEngine();
            var @lock = engine.CreateReaderWriterLock(nameof(TestReaderWriterLockBadArguments));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireReadLock(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireReadLockAsync(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireReadLock(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireReadLockAsync(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireReadLock(TimeSpan.FromSeconds(int.MaxValue)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireReadLockAsync(TimeSpan.FromSeconds(int.MaxValue)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireReadLock(TimeSpan.FromSeconds(int.MaxValue)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireReadLockAsync(TimeSpan.FromSeconds(int.MaxValue)));

            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireUpgradeableReadLock(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireUpgradeableReadLockAsync(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireUpgradeableReadLock(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireUpgradeableReadLockAsync(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireUpgradeableReadLock(TimeSpan.FromSeconds(int.MaxValue)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireUpgradeableReadLockAsync(TimeSpan.FromSeconds(int.MaxValue)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireUpgradeableReadLock(TimeSpan.FromSeconds(int.MaxValue)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireUpgradeableReadLockAsync(TimeSpan.FromSeconds(int.MaxValue)));

            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireWriteLock(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireWriteLockAsync(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireWriteLock(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireWriteLockAsync(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireWriteLock(TimeSpan.FromSeconds(int.MaxValue)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireWriteLockAsync(TimeSpan.FromSeconds(int.MaxValue)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireWriteLock(TimeSpan.FromSeconds(int.MaxValue)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireWriteLockAsync(TimeSpan.FromSeconds(int.MaxValue)));

            using var upgradeableHandle = @lock.AcquireUpgradeableReadLock();
            Assert.Catch<ArgumentOutOfRangeException>(() => upgradeableHandle.UpgradeToWriteLock(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => upgradeableHandle.UpgradeToWriteLockAsync(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => upgradeableHandle.TryUpgradeToWriteLock(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => upgradeableHandle.TryUpgradeToWriteLockAsync(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => upgradeableHandle.UpgradeToWriteLock(TimeSpan.FromSeconds(int.MaxValue)));
            Assert.Catch<ArgumentOutOfRangeException>(() => upgradeableHandle.UpgradeToWriteLockAsync(TimeSpan.FromSeconds(int.MaxValue)));
            Assert.Catch<ArgumentOutOfRangeException>(() => upgradeableHandle.TryUpgradeToWriteLock(TimeSpan.FromSeconds(int.MaxValue)));
            Assert.Catch<ArgumentOutOfRangeException>(() => upgradeableHandle.TryUpgradeToWriteLockAsync(TimeSpan.FromSeconds(int.MaxValue)));
        }

        [Test]
        public void TestUpgradeableHandleDisposal()
        {
            using var engine = this.CreateEngine();
            var @lock = engine.CreateReaderWriterLock(nameof(TestUpgradeableHandleDisposal));

            var handle = @lock.AcquireUpgradeableReadLock();
            handle.Dispose();
            Assert.DoesNotThrow(() => handle.Dispose());
            Assert.Catch<ObjectDisposedException>(() => handle.TryUpgradeToWriteLock());
            Assert.Catch<ObjectDisposedException>(() => handle.TryUpgradeToWriteLockAsync());
            Assert.Catch<ObjectDisposedException>(() => handle.UpgradeToWriteLock());
            Assert.Catch<ObjectDisposedException>(() => handle.UpgradeToWriteLockAsync());
        }

        [Test]
        public void TestUpgradeableHandleMultipleUpgrades()
        {
            using var engine = this.CreateEngine();
            var @lock = engine.CreateReaderWriterLock(nameof(TestUpgradeableHandleMultipleUpgrades));

            using var upgradeHandle = @lock.AcquireUpgradeableReadLock();
            upgradeHandle.UpgradeToWriteLock();
            Assert.Catch<InvalidOperationException>(() => upgradeHandle.TryUpgradeToWriteLock());
        }

        private TestingSqlDistributedReaderWriterLockEngine<TConnectionManagementProvider> CreateEngine() =>
            new TestingSqlDistributedReaderWriterLockEngine<TConnectionManagementProvider>();
    }
}
