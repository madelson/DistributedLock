using Medallion.Threading.InProcess;
using NUnit.Framework;

namespace Medallion.Threading.Tests.InProcess;

public class InProcessPrimitiveCacheTest
{
    /// <summary>
    /// The provider caches its underlying primitives by name using weak references. If that cache ever hands out
    /// two different primitives for the same name, mutual exclusion is silently lost.
    /// </summary>
    [Test]
    public void TestSameNameAlwaysYieldsMutualExclusionUnderConcurrency()
    {
        var provider = new InProcessDistributedSynchronizationProvider();
        const string LockName = TargetFramework.Current + "PrimitiveCacheRace";

        var threadCount = Math.Max(4, Environment.ProcessorCount);
        using var barrier = new Barrier(threadCount);
        var concurrentHolders = 0;
        var maxConcurrentHolders = 0;

        Parallel.For(0, threadCount, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, _ =>
        {
            // maximize the chance of racing inside GetOrAdd
            barrier.SignalAndWait();

            for (var i = 0; i < 200; ++i)
            {
                using (provider.AcquireLock(LockName))
                {
                    var holders = Interlocked.Increment(ref concurrentHolders);
                    InterlockedMax(ref maxConcurrentHolders, holders);
                    Interlocked.Decrement(ref concurrentHolders);
                }

                // give the cache a chance to see collectible entries
                if (i % 50 == 0) { GC.Collect(); }
            }
        });

        maxConcurrentHolders.ShouldEqual(1, "the same name must always map to the same underlying primitive");
    }

    /// <summary>
    /// Once all references to a name's primitive are dropped it may be collected, but re-acquiring the same
    /// name must still produce a working lock.
    /// </summary>
    [Test]
    public void TestNameIsReusableAfterCollection()
    {
        var provider = new InProcessDistributedSynchronizationProvider();
        const string LockName = TargetFramework.Current + "PrimitiveCacheReuse";

        provider.AcquireLock(LockName).Dispose();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        using var handle = provider.AcquireLock(LockName);
        Assert.That(handle, Is.Not.Null);
        Assert.That(provider.TryAcquireLock(LockName), Is.Null, "the recreated primitive must still be exclusive");
    }

    private static void InterlockedMax(ref int target, int value)
    {
        var current = Volatile.Read(ref target);
        while (value > current)
        {
            var found = Interlocked.CompareExchange(ref target, value, current);
            if (found == current) { return; }
            current = found;
        }
    }
}
