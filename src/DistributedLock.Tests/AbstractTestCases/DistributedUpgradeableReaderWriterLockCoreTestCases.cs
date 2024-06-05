using NUnit.Framework;

namespace Medallion.Threading.Tests;

public abstract class DistributedUpgradeableReaderWriterLockCoreTestCases<TLockProvider, TStrategy>
    where TLockProvider : TestingUpgradeableReaderWriterLockProvider<TStrategy>, new()
    where TStrategy : TestingSynchronizationStrategy, new()
{
    private TLockProvider _lockProvider = default!;

    [SetUp]
    public async Task SetUp()
    {
        this._lockProvider = new TLockProvider();
        await this._lockProvider.SetupAsync();
    }
    [TearDown]
    public async Task TearDown() => await this._lockProvider.DisposeAsync();

    [Test]
    public void TestMultipleReadersSingleWriter()
    {
        IDistributedUpgradeableReaderWriterLock Lock() =>
            this._lockProvider.CreateUpgradeableReaderWriterLock(nameof(TestMultipleReadersSingleWriter));

        using var readHandle1 = Lock().TryAcquireReadLockAsync().AsTask().Result;
        Assert.That(readHandle1, Is.Not.Null, this.GetType().ToString());
        using var readHandle2 = Lock().TryAcquireReadLock();
        Assert.That(readHandle2, Is.Not.Null, this.GetType().ToString());

        using (var handle = Lock().TryAcquireUpgradeableReadLock())
        {
            Assert.That(handle, Is.Not.Null);

            using var readHandle3 = Lock().TryAcquireReadLock();
            Assert.That(readHandle3, Is.Not.Null);

            Lock().TryAcquireUpgradeableReadLock().ShouldEqual(null);
            Lock().TryAcquireWriteLock().ShouldEqual(null);

            readHandle3!.Dispose();
        }

        readHandle1!.Dispose();
        readHandle2!.Dispose();

        using var upgradeHandle = Lock().TryAcquireUpgradeableReadLock();
        Assert.That(upgradeHandle, Is.Not.Null);
    }

    [Test]
    public void TestUpgradeToWriteLock()
    {
        IDistributedUpgradeableReaderWriterLock Lock() =>
            this._lockProvider.CreateUpgradeableReaderWriterLock(nameof(TestUpgradeToWriteLock));

        var readHandle = Lock().AcquireReadLock();

        Task<IDistributedSynchronizationHandle> readTask;
        using (var upgradeableHandle = Lock().AcquireUpgradeableReadLockAsync().AsTask().Result)
        {
            upgradeableHandle.TryUpgradeToWriteLock().ShouldEqual(false); // read lock still held

            readHandle.Dispose();

            upgradeableHandle.TryUpgradeToWriteLock().ShouldEqual(true);

            readTask = Task.Run(() => Lock().AcquireReadLockAsync().AsTask());
            readTask.Wait(TimeSpan.FromSeconds(.1)).ShouldEqual(false, "write lock held");
        }

        readTask.Wait(TimeSpan.FromSeconds(10)).ShouldEqual(true, "write lock released");
        readTask.Result.Dispose();
    }

    [Test]
    public void TestReaderWriterLockBadArguments()
    {
        var @lock = this._lockProvider.CreateUpgradeableReaderWriterLock(nameof(TestReaderWriterLockBadArguments));
        Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireUpgradeableReadLock(TimeSpan.FromSeconds(-2)));
        Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireUpgradeableReadLockAsync(TimeSpan.FromSeconds(-2)));
        Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireUpgradeableReadLock(TimeSpan.FromSeconds(-2)));
        Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireUpgradeableReadLockAsync(TimeSpan.FromSeconds(-2)));
        Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireUpgradeableReadLock(TimeSpan.FromSeconds(int.MaxValue)));
        Assert.Catch<ArgumentOutOfRangeException>(() => @lock.AcquireUpgradeableReadLockAsync(TimeSpan.FromSeconds(int.MaxValue)));
        Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireUpgradeableReadLock(TimeSpan.FromSeconds(int.MaxValue)));
        Assert.Catch<ArgumentOutOfRangeException>(() => @lock.TryAcquireUpgradeableReadLockAsync(TimeSpan.FromSeconds(int.MaxValue)));

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
        var @lock = this._lockProvider.CreateUpgradeableReaderWriterLock(nameof(TestUpgradeableHandleDisposal));

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
        var @lock = this._lockProvider.CreateUpgradeableReaderWriterLock(nameof(TestUpgradeableHandleMultipleUpgrades));

        using var upgradeHandle = @lock.AcquireUpgradeableReadLock();
        upgradeHandle.UpgradeToWriteLock();
        Assert.Catch<InvalidOperationException>(() => upgradeHandle.TryUpgradeToWriteLock());
    }

    [Test]
    public async Task TestCanUpgradeHandleWhileMonitoring()
    {
        var handleLostHelper = this._lockProvider.Strategy.PrepareForHandleLost();

        var @lock = this._lockProvider.CreateUpgradeableReaderWriterLock(nameof(TestCanUpgradeHandleWhileMonitoring));

        using var handle = await @lock.AcquireUpgradeableReadLockAsync();

        // start monitoring
        using var canceledEvent = new ManualResetEventSlim(initialState: false);
        using var registration = handle.HandleLostToken.Register(canceledEvent.Set);
        Assert.That(canceledEvent.Wait(TimeSpan.FromSeconds(.05)), Is.False);

        Assert.DoesNotThrowAsync(() => handle.UpgradeToWriteLockAsync().AsTask());

        Assert.That(canceledEvent.Wait(TimeSpan.FromSeconds(.05)), Is.False);

        if (handleLostHelper != null)
        {
            handleLostHelper.Dispose();
            Assert.That(canceledEvent.Wait(TimeSpan.FromSeconds(10)), Is.True);
        }

        // when the handle is lost, Dispose() may throw
        try { await handle.DisposeAsync(); }
        catch { }
    }
}
