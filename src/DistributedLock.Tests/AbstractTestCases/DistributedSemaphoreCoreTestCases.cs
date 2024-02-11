using NUnit.Framework;

namespace Medallion.Threading.Tests;

public abstract class DistributedSemaphoreCoreTestCases<TSemaphoreProvider, TStrategy>
    where TSemaphoreProvider : TestingSemaphoreProvider<TStrategy>, new()
    where TStrategy : TestingSynchronizationStrategy, new()
{
    private static readonly TimeSpan LongTimeout = TimeSpan.FromSeconds(5);

    private TSemaphoreProvider _semaphoreProvider = default!;

    [SetUp] public void SetUp() => this._semaphoreProvider = new TSemaphoreProvider();
    [TearDown] public void TearDown() => this._semaphoreProvider.Dispose();

    [Test]
    public void TestMaxCount()
    {
        this._semaphoreProvider.CreateSemaphore(string.Empty, 5).MaxCount.ShouldEqual(5);
        this._semaphoreProvider.CreateSemaphore(string.Empty, 23).MaxCount.ShouldEqual(23);
    }

    [Test]
    [NonParallelizable] // timing-sensitive
    public void TestConcurrencyHandling()
    {
        const int MaxCount = 3;

        var counter = 0;
        var seenCounterValues = new HashSet<int>();

        const int Threads = 10;
        const int Trials = 25;
        var barrier = new Barrier(Threads);
        var threads = Enumerable.Range(0, Threads)
            .Select(_ => Task.Factory.StartNew(() =>
                {
                    var semaphore = this._semaphoreProvider.CreateSemaphore(nameof(TestConcurrencyHandling), MaxCount);

                    barrier.SignalAndWait();
                    for (var i = 0; i < Trials; ++i)
                    {
                        using var _ = semaphore.Acquire(LongTimeout);
                        var newCounterValue = Interlocked.Increment(ref counter);
                        lock (seenCounterValues) { seenCounterValues.Add(newCounterValue); }
                        Thread.Sleep(10);
                        Interlocked.Decrement(ref counter);
                    }
                }, 
                TaskCreationOptions.LongRunning // dedicated thread
            ))
            .ToArray();
        Task.WaitAll(threads);

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, seenCounterValues.ToArray());
    }

    [Test]
    public void TestDrain()
    {
        var semaphore = this._semaphoreProvider.CreateSemaphore(nameof(TestDrain), maxCount: 4);
        var semaphore2 = this._semaphoreProvider.CreateSemaphore(nameof(TestDrain), maxCount: 4);

        var handles = new List<IDisposable> { semaphore.Acquire(LongTimeout) };
        Assert.DoesNotThrow(() => semaphore2.Acquire().Dispose());
        while (handles.Count < 4) { handles.Add(semaphore.Acquire(LongTimeout)); }

        semaphore2.TryAcquire().ShouldEqual(null);
        semaphore.TryAcquire().ShouldEqual(null);

        handles[0].Dispose();
        Assert.DoesNotThrow(() => semaphore2.Acquire().Dispose());

        handles.ForEach(h => h.Dispose());
    }

    [Test]
    public void TestHighTicketCount()
    {
        var semaphore = this._semaphoreProvider.CreateSemaphore($"s{new string('o', 1000)} many tickets!", int.MaxValue);
        var handles = Enumerable.Range(0, 100)
            .Select(_ => semaphore.Acquire(LongTimeout))
            .ToList();
        handles.ForEach(h => h.Dispose());
    }

    [Test]
    [NonParallelizable, Retry(tryCount: 3)] // somewhat perf-sensitive
    public void TestSemaphoreParallelism()
    {
        const int MaxCount = 10;

        var counter = 0;
        var maxCounterValue = 0;
        var maxCounterValueLock = new object();
        var tasks = Enumerable.Range(1, 100).Select(async _ =>
            {
                var semaphore = this._semaphoreProvider.CreateSemaphore(nameof(TestSemaphoreParallelism), MaxCount);
                using (await semaphore.AcquireAsync())
                {
                    // increment going in
                    var currentCounterValue = Interlocked.Increment(ref counter);

                    lock (maxCounterValueLock)
                    {
                        maxCounterValue = Math.Max(maxCounterValue, currentCounterValue);
                    }

                    // hang out for a bit to ensure concurrency
                    await Task.Delay(TimeSpan.FromMilliseconds(30));

                    // decrement and return on the way out (returns # inside the lock when this left ... should be 0)
                    return Interlocked.Decrement(ref counter);
                }
            })
            .ToList();

        Task.WaitAll(tasks.ToArray<Task>(), TimeSpan.FromSeconds(30)).ShouldEqual(true, this.GetType().Name);

        tasks.ForEach(t =>
        {
            (t.Result >= 0).ShouldEqual(true);
            (t.Result <= MaxCount).ShouldEqual(true);
        });
        Volatile.Read(ref counter).ShouldEqual(0);

        lock (maxCounterValueLock)
        {
            maxCounterValue.ShouldEqual(MaxCount, this.GetType().Name + ": should reach the maximum level of allowed concurrency");
        }
    }
}
