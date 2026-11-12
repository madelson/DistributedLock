using Medallion.Threading.InProcess;
using NUnit.Framework;

namespace Medallion.Threading.Tests.InProcess;

public class InProcessDistributedSynchronizationProviderTest
{
    [Test]
    public async Task BasicTest()
    {
        var provider = new InProcessDistributedSynchronizationProvider();
        const string LockName = TargetFramework.Current + "ProviderBasicTest";
        await using (await provider.AcquireLockAsync(LockName))
        {
            await using var handle = await provider.TryAcquireLockAsync(LockName);
            Assert.That(handle, Is.Null);
        }

        const string ReaderWriterLockName = TargetFramework.Current + "ProviderBasicTest_ReaderWriter";
        await using (await provider.AcquireReadLockAsync(ReaderWriterLockName))
        {
            await using var handle = await provider.TryAcquireWriteLockAsync(ReaderWriterLockName);
            Assert.That(handle, Is.Null);
        }

        await using (await provider.AcquireReadLockAsync(ReaderWriterLockName))
        {
            await using var handle = await provider.TryAcquireReadLockAsync(ReaderWriterLockName);
            Assert.That(handle, Is.Not.Null);
        }

        await using (var upgradeHandle = await provider.AcquireUpgradeableReadLockAsync(ReaderWriterLockName))
        {
            await using var handle = await provider.TryAcquireReadLockAsync(ReaderWriterLockName);
            Assert.That(handle, Is.Not.Null);
            var upgraded = upgradeHandle.TryUpgradeToWriteLockAsync();
            Assert.That(await upgraded, Is.False);
        }

        const string SemaphoreName = TargetFramework.Current + "ProviderBasicTest_Semaphore";
        await using (await provider.AcquireSemaphoreAsync(SemaphoreName, 2))
        {
            await using var handle = await provider.TryAcquireSemaphoreAsync(SemaphoreName, 2);
            Assert.That(handle, Is.Not.Null);

            await using var failedHandle = await provider.TryAcquireSemaphoreAsync(SemaphoreName, 2);
            Assert.That(failedHandle, Is.Null);
        }
    }

    [Test]
    public void TestDifferentScopesAreIsolated()
    {
        var providerA = new InProcessDistributedSynchronizationProvider(NewScope());
        var providerB = new InProcessDistributedSynchronizationProvider(NewScope());
        const string Name = "ScopeIsolation";

        using var handleA = providerA.AcquireLock(Name);
        using var handleB = providerB.TryAcquireLock(Name);
        Assert.That(handleB, Is.Not.Null);
    }

    [Test]
    public void TestSameScopeIsSharedAcrossInstances()
    {
        var scope = NewScope();
        var provider1 = new InProcessDistributedSynchronizationProvider(scope);
        var provider2 = new InProcessDistributedSynchronizationProvider(scope);
        Assert.That(provider2.Scope, Is.EqualTo(scope));
        const string Name = "ScopeSharing";

        using var handle = provider1.AcquireLock(Name);
        using var handle2 = provider2.TryAcquireLock(Name);
        Assert.That(handle2, Is.Null);
    }

    [Test]
    public void TestDefaultScopeIsShared()
    {
        var name = "DefaultScope" + Guid.NewGuid().ToString("N");
        var provider1 = new InProcessDistributedSynchronizationProvider();
        var provider2 = new InProcessDistributedSynchronizationProvider();

        using var handle = provider1.AcquireLock(name);
        using var handle2 = provider2.TryAcquireLock(name);
        Assert.That(handle2, Is.Null);
    }

    [Test]
    public async Task TestZeroTimeoutReadSucceedsUnderReaderChurn()
    {
        var provider = new InProcessDistributedSynchronizationProvider(NewScope());
        const string Name = "ReaderChurn";

        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var churners = Enumerable.Range(0, Math.Max(2, Environment.ProcessorCount))
            .Select(_ => Task.Run(() =>
            {
                while (!stop.IsCancellationRequested)
                {
                    using var handle = provider.AcquireReadLock(Name);
                }
            }))
            .ToArray();

        var failures = 0;
        while (!stop.IsCancellationRequested)
        {
            using var handle = provider.TryAcquireReadLock(Name);
            if (handle == null) { ++failures; }
        }
        await Task.WhenAll(churners);

        Assert.That(failures, Is.EqualTo(0), "readers must never block zero-timeout readers when no writer is present");
    }

    [Test]
    public async Task TestWaitingWriterBlocksNewReaders()
    {
        var provider = new InProcessDistributedSynchronizationProvider(NewScope());
        const string Name = "WriterPriority";

        var readHandle = provider.AcquireReadLock(Name);
        var writeTask = provider.AcquireWriteLockAsync(Name).AsTask();
        await Task.Delay(100);
        Assert.That(writeTask.IsCompleted, Is.False);

        using (var blockedRead = provider.TryAcquireReadLock(Name))
        {
            Assert.That(blockedRead, Is.Null, "a waiting writer must take priority over new readers");
        }

        readHandle.Dispose();
        await using (await writeTask) { }

        using var readAfter = provider.TryAcquireReadLock(Name);
        Assert.That(readAfter, Is.Not.Null);
    }

    [Test]
    public async Task TestFailedWriterDoesNotBlockReaders()
    {
        var provider = new InProcessDistributedSynchronizationProvider(NewScope());
        const string Name = "FailedWriter";

        using (provider.AcquireReadLock(Name))
        {
            await using var write = await provider.TryAcquireWriteLockAsync(Name, TimeSpan.FromMilliseconds(50));
            Assert.That(write, Is.Null);

            using var read = provider.TryAcquireReadLock(Name);
            Assert.That(read, Is.Not.Null, "a timed-out writer must not leave readers on the slow path");
        }
    }

    [Test]
    public async Task TestReadFastPathRecoversAfterUpgrade()
    {
        var provider = new InProcessDistributedSynchronizationProvider(NewScope());
        const string Name = "UpgradeRecovery";

        await using (var upgradeable = await provider.AcquireUpgradeableReadLockAsync(Name))
        {
            await upgradeable.UpgradeToWriteLockAsync();
            using var blocked = provider.TryAcquireReadLock(Name);
            Assert.That(blocked, Is.Null);
        }

        using var read1 = provider.TryAcquireReadLock(Name);
        using var read2 = provider.TryAcquireReadLock(Name);
        Assert.That(read1, Is.Not.Null);
        Assert.That(read2, Is.Not.Null);
    }

    private static string NewScope() => Guid.NewGuid().ToString("N");
}
