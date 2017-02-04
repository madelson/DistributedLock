using Medallion.Threading.Sql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Medallion.Threading.Tests.Sql
{
    public abstract class SqlDistributedReaderWriterLockTestBase : DistributedLockTestBase
    {
        [TestMethod]
        public void TestMultipleReadersSingleWriter()
        {
            var @lock = this.CreateReaderWriterLock(nameof(TestMultipleReadersSingleWriter));
            
            var readHandle1 = this.CreateReaderWriterLock(nameof(TestMultipleReadersSingleWriter)).TryAcquireReadLockAsync().Result;
            Assert.IsNotNull(readHandle1, this.GetType().ToString());
            var readHandle2 = this.CreateReaderWriterLock(nameof(TestMultipleReadersSingleWriter)).TryAcquireReadLockAsync().Result;
            Assert.IsNotNull(readHandle2, this.GetType().ToString());

            using (var handle = @lock.TryAcquireUpgradeableReadLock())
            {
                Assert.IsNotNull(handle);

                var readHandle3 = this.CreateReaderWriterLock(nameof(TestMultipleReadersSingleWriter)).TryAcquireReadLock();
                Assert.IsNotNull(readHandle3);

                this.CreateReaderWriterLock(nameof(TestMultipleReadersSingleWriter)).TryAcquireUpgradeableReadLock().ShouldEqual(null);
                this.CreateReaderWriterLock(nameof(TestMultipleReadersSingleWriter)).TryAcquireWriteLock().ShouldEqual(null);

                readHandle3.Dispose();
            }

            readHandle1.Dispose();
            readHandle2.Dispose();

            using (var writeHandle = this.CreateReaderWriterLock(nameof(TestMultipleReadersSingleWriter)).TryAcquireUpgradeableReadLock())
            {
                Assert.IsNotNull(writeHandle);
            }
        }

        [TestMethod]
        public void TestUpgradeToWriteLock()
        {
            var @lock = this.CreateReaderWriterLock(nameof(TestUpgradeToWriteLock));

            var readHandle = this.CreateReaderWriterLock(nameof(TestUpgradeToWriteLock)).AcquireReadLock();

            Task<IDisposable> readTask;
            using (var upgradeableHandle = @lock.AcquireUpgradeableReadLockAsync().Result)
            {
                upgradeableHandle.TryUpgradeToWriteLock().ShouldEqual(false); // read lock still held

                readHandle.Dispose();

                upgradeableHandle.TryUpgradeToWriteLock().ShouldEqual(true);

                readTask = this.CreateReaderWriterLock(nameof(TestUpgradeToWriteLock)).AcquireReadLockAsync();
                readTask.Wait(TimeSpan.FromSeconds(.1)).ShouldEqual(false, "write lock held");
            }

            readTask.Wait(TimeSpan.FromSeconds(10)).ShouldEqual(true, "write lock released");
            readTask.Result.Dispose();
        }

        [TestMethod]
        public void TestReaderWriterLockBadArguments()
        {
            var @lock = this.CreateReaderWriterLock(Guid.NewGuid().ToString());
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

        [TestMethod]
        public void TestUpgradeableHandleDisposal()
        {
            var @lock = this.CreateReaderWriterLock(nameof(TestUpgradeableHandleDisposal));

            var handle = @lock.AcquireUpgradeableReadLock();
            handle.Dispose();
            TestHelper.AssertDoesNotThrow(() => handle.Dispose());
            TestHelper.AssertThrows<ObjectDisposedException>(() => handle.TryUpgradeToWriteLock());
            TestHelper.AssertThrows<ObjectDisposedException>(() => handle.TryUpgradeToWriteLockAsync());
            TestHelper.AssertThrows<ObjectDisposedException>(() => handle.UpgradeToWriteLock());
            TestHelper.AssertThrows<ObjectDisposedException>(() => handle.UpgradeToWriteLockAsync());
        }

        [TestMethod]
        public void TestUpgradeableHandleMultipleUpgrades()
        {
            var @lock = this.CreateReaderWriterLock(nameof(TestUpgradeableHandleMultipleUpgrades));

            using (var upgradeHandle = @lock.AcquireUpgradeableReadLock())
            {
                upgradeHandle.UpgradeToWriteLock();
                TestHelper.AssertThrows<InvalidOperationException>(() => upgradeHandle.TryUpgradeToWriteLock());
            }
        }
        
        internal virtual bool UseWriteLockAsExclusive() => true;

        internal abstract SqlDistributedReaderWriterLock CreateReaderWriterLock(string name);

        internal sealed override IDistributedLock CreateLock(string name) => new SqlReaderWriterLockDistributedLock(this, name);

        internal sealed override string GetSafeLockName(string name) => SqlDistributedReaderWriterLock.GetSafeLockName(name);

        private sealed class SqlReaderWriterLockDistributedLock : IDistributedLock
        {
            private readonly SqlDistributedReaderWriterLock @lock;
            private readonly SqlDistributedReaderWriterLockTestBase test;

            public SqlReaderWriterLockDistributedLock(SqlDistributedReaderWriterLockTestBase test, string name)
            {
                this.@lock = test.CreateReaderWriterLock(name);
                this.test = test;
            }

            public IDisposable Acquire(TimeSpan? timeout = default(TimeSpan?), CancellationToken cancellationToken = default(CancellationToken))
            {
                return test.UseWriteLockAsExclusive() 
                    ? this.@lock.AcquireWriteLock(timeout, cancellationToken)
                    : this.@lock.AcquireUpgradeableReadLock(timeout, cancellationToken);
            }

            public Task<IDisposable> AcquireAsync(TimeSpan? timeout = default(TimeSpan?), CancellationToken cancellationToken = default(CancellationToken))
            {
                return test.UseWriteLockAsExclusive()
                    ? this.@lock.AcquireWriteLockAsync(timeout, cancellationToken)
                    : CastToDisposable(this.@lock.AcquireUpgradeableReadLockAsync(timeout, cancellationToken));
            }

            public IDisposable TryAcquire(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
            {
                return test.UseWriteLockAsExclusive()
                    ? this.@lock.TryAcquireWriteLock(timeout, cancellationToken)
                    : this.@lock.TryAcquireUpgradeableReadLock(timeout, cancellationToken);
            }

            public Task<IDisposable> TryAcquireAsync(TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
            {
                return test.UseWriteLockAsExclusive()
                    ? this.@lock.TryAcquireWriteLockAsync(timeout, cancellationToken)
                    : CastToDisposable(this.@lock.TryAcquireUpgradeableReadLockAsync(timeout, cancellationToken));
            }

            private static async Task<IDisposable> CastToDisposable<T>(Task<T> task) where T : IDisposable
            {
                return await task.ConfigureAwait(false);
            }
        }
    }
}
