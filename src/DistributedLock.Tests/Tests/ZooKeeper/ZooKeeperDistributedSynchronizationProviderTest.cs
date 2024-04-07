using Medallion.Threading.ZooKeeper;
using NUnit.Framework;

namespace Medallion.Threading.Tests.ZooKeeper;

public class ZooKeeperDistributedSynchronizationProviderTest
{
    [Test, Category("CI")]
    public void TestArgumentValidation()
    {
        Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedSynchronizationProvider(default, ZooKeeperPorts.DefaultConnectionString));
        Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedSynchronizationProvider(null!));
    }

    [Test]
    public async Task BasicTest()
    {
        var provider = new ZooKeeperDistributedSynchronizationProvider(ZooKeeperPorts.DefaultConnectionString);

        var lockName = TestHelper.UniqueName + "Lock";
        await using (await provider.AcquireLockAsync(lockName))
        {
            await using var handle = await provider.TryAcquireLockAsync(lockName);
            Assert.That(handle, Is.Null);
        }

        var readerWriterLockName = TestHelper.UniqueName + "ReaderWriterLock";
        await using (await provider.AcquireReadLockAsync(readerWriterLockName))
        {
            await using var handle = await provider.TryAcquireWriteLockAsync(readerWriterLockName);
            Assert.That(handle, Is.Null);
        }

        var semaphoreName = TestHelper.UniqueName + "Semaphore";
        await using (await provider.AcquireSemaphoreAsync(semaphoreName, 2))
        {
            await using var handle = await provider.TryAcquireSemaphoreAsync(semaphoreName, 2);
            Assert.That(handle, Is.Not.Null);

            await using var failedHandle = await provider.TryAcquireSemaphoreAsync(semaphoreName, 2);
            Assert.That(failedHandle, Is.Null);
        }
    }

    [Test]
    public async Task TestDifferentPrimitivesDoNotCollide()
    {
        var provider = new ZooKeeperDistributedSynchronizationProvider(ZooKeeperPorts.DefaultConnectionString);
        
        var name = TestHelper.UniqueName;
        var @lock = provider.CreateLock(name);
        var readerWriterLock = provider.CreateReaderWriterLock(name);
        var semaphore = provider.CreateSemaphore(name, maxCount: 1);

        @lock.Path.ShouldEqual(readerWriterLock.Path);
        @lock.Path.ShouldEqual(semaphore.Path);

        await using var lockHandle = await @lock.TryAcquireAsync();
        Assert.That(lockHandle, Is.Not.Null);
        await using var readLockHandle = await readerWriterLock.TryAcquireReadLockAsync();
        Assert.That(readLockHandle, Is.Not.Null);
        await using var semaphoreHandle = await semaphore.TryAcquireAsync();
        Assert.That(semaphoreHandle, Is.Not.Null);

        await readLockHandle!.DisposeAsync();
        await using var writeLockHandle = await readerWriterLock.TryAcquireWriteLockAsync();
        Assert.That(writeLockHandle, Is.Not.Null);
    }

    [Test, Category("CI")]
    public void TestIncorporatesDirectoryNameIfProvided()
    {
        var provider = new ZooKeeperDistributedSynchronizationProvider(new ZooKeeperPath("/foo"), ZooKeeperPorts.DefaultConnectionString);
        provider.CreateLock("bar").Path.ToString().ShouldEqual("/foo/bar");
        provider.CreateReaderWriterLock("baz").Path.ToString().ShouldEqual("/foo/baz");
        provider.CreateSemaphore("qux", 1).Path.ToString().ShouldEqual("/foo/qux");
    } 
}
