using NUnit.Framework;

namespace Medallion.Threading.Tests;

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
        Assert.That(readHandle1, Is.Not.Null, this.GetType().ToString());
        using var readHandle2 = Lock().TryAcquireReadLock();
        Assert.That(readHandle2, Is.Not.Null, this.GetType().ToString());

        using var writeHandle1 = Lock().TryAcquireWriteLock();
        Assert.That(writeHandle1, Is.Null);

        var writeHandleTask = Task.Run(() => Lock().AcquireWriteLockAsync().AsTask());
        Assert.That(writeHandleTask.Wait(TimeSpan.FromSeconds(.05)), Is.False);

        readHandle1!.Dispose();
        Assert.That(writeHandleTask.Wait(TimeSpan.FromSeconds(.05)), Is.False);

        readHandle2!.Dispose();
        Assert.That(writeHandleTask.Wait(TimeSpan.FromSeconds(10)), Is.True);
        using var writeHandle2 = writeHandleTask.Result;

        using var writeHandle3 = Lock().TryAcquireWriteLock();
        Assert.That(writeHandle3, Is.Null);

        writeHandle2.Dispose();

        using var writeHandle4 = Lock().TryAcquireWriteLock();
        Assert.That(writeHandle4, Is.Not.Null);
    }

    [Test]
    public async Task TestWriterTrumpsReader()
    {
        IDistributedReaderWriterLock Lock() =>
            this._lockProvider.CreateReaderWriterLock(nameof(this.TestWriterTrumpsReader));

        await using var readerHandle = await Lock().AcquireReadLockAsync();

        var writerHandleTask = Task.Run(() => Lock().AcquireWriteLockAsync().AsTask());
        Assert.That(await writerHandleTask.TryWaitAsync(TimeSpan.FromSeconds(0.2)), Is.False);

        // trying to take a read lock here fails because there is a writer waiting
        await using var readerHandle2 = await Lock().TryAcquireReadLockAsync();
        Assert.That(readerHandle2, Is.Null);

        await readerHandle.DisposeAsync();

        Assert.That(await writerHandleTask.TryWaitAsync(TimeSpan.FromSeconds(5)), Is.True);
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
