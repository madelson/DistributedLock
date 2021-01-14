using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests
{
    public abstract class DistributedReaderWriterLockCoreTestCases<TLockProvider, TStrategy>
        where TLockProvider : TestingReaderWriterLockProvider<TStrategy>, new()
        where TStrategy : TestingSynchronizationStrategy, new()
    {
        private TLockProvider _lockProvider = default!;

        [SetUp] public void SetUp() => this._lockProvider = new TLockProvider();
        [TearDown] public void TearDown() => this._lockProvider.Dispose();

        [Test]
        public async Task TestMultipleReadersSingleWriter()
        {
            IDistributedReaderWriterLock Lock() =>
                this._lockProvider.CreateReaderWriterLock(nameof(TestMultipleReadersSingleWriter));

            using var readHandle1 = await Lock().TryAcquireReadLockAsync();
            Assert.IsNotNull(readHandle1, this.GetType().ToString());
            using var readHandle2 = Lock().TryAcquireReadLock();
            Assert.IsNotNull(readHandle2, this.GetType().ToString());

            using var writeHandle1 = Lock().TryAcquireWriteLock();
            Assert.IsNull(writeHandle1);

            var writeHandleTask = Lock().AcquireWriteLockAsync().AsTask();
            Assert.IsFalse(writeHandleTask.Wait(TimeSpan.FromSeconds(.05)));

            readHandle1!.Dispose();
            Assert.IsFalse(writeHandleTask.Wait(TimeSpan.FromSeconds(.05)));

            readHandle2!.Dispose();
            Assert.IsTrue(writeHandleTask.Wait(TimeSpan.FromSeconds(10)));
            using var writeHandle2 = writeHandleTask.Result;

            using var writeHandle3 = Lock().TryAcquireWriteLock();
            Assert.IsNull(writeHandle3);

            writeHandle2.Dispose();

            using var writeHandle4 = Lock().TryAcquireWriteLock();
            Assert.IsNotNull(writeHandle4);
        }

        [Test]
        public async Task TestWriterTrumpsReader()
        {
            IDistributedReaderWriterLock Lock() =>
                this._lockProvider.CreateReaderWriterLock(nameof(this.TestWriterTrumpsReader));

            await using var readerHandle = await Lock().AcquireReadLockAsync();

            var writerHandleTask = Lock().AcquireWriteLockAsync().AsTask();
            Assert.IsFalse(await writerHandleTask.WaitAsync(TimeSpan.FromSeconds(0.2)));

            // trying to take a read lock here fails because there is a writer waiting
            await using var readerHandle2 = await Lock().TryAcquireReadLockAsync();
            Assert.IsNull(readerHandle2);

            await readerHandle.DisposeAsync();

            Assert.IsTrue(await writerHandleTask.WaitAsync(TimeSpan.FromSeconds(5)));
            await writerHandleTask.Result.DisposeAsync();
        }

        [Test]
        public void TestReaderWriterLockBadArguments()
        {
            var @lock = this._lockProvider.CreateReaderWriterLock(nameof(TestReaderWriterLockBadArguments));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireReadLock(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireReadLockAsync(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireReadLock(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireReadLockAsync(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireReadLock(TimeSpan.FromSeconds(int.MaxValue)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireReadLockAsync(TimeSpan.FromSeconds(int.MaxValue)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireReadLock(TimeSpan.FromSeconds(int.MaxValue)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireReadLockAsync(TimeSpan.FromSeconds(int.MaxValue)));

            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireWriteLock(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireWriteLockAsync(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireWriteLock(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireWriteLockAsync(TimeSpan.FromSeconds(-2)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireWriteLock(TimeSpan.FromSeconds(int.MaxValue)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireWriteLockAsync(TimeSpan.FromSeconds(int.MaxValue)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireWriteLock(TimeSpan.FromSeconds(int.MaxValue)));
            Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireWriteLockAsync(TimeSpan.FromSeconds(int.MaxValue)));
        }
    }
}
