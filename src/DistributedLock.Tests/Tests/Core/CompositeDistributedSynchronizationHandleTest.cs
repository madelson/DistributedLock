using Medallion.Threading.Chaining;
using Moq;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Core;

[Category("CI")]
public class CompositeDistributedSynchronizationHandleTest
{
    [Test]
    public void TestDisposeMultipleTimes()
    {
        var handle1 = new Mock<IDistributedSynchronizationHandle>();
        var handle2 = new Mock<IDistributedSynchronizationHandle>();
        var sut = AcquireAll(Succeeds(handle1), Succeeds(handle2));

        sut.Dispose();
        handle1.Verify(h => h.Dispose(), Times.Once);
        handle2.Verify(h => h.Dispose(), Times.Once);
        sut.Dispose();
        handle1.Verify(h => h.Dispose(), Times.Once);
        handle2.Verify(h => h.Dispose(), Times.Once);

        // HandleLostToken can be expensive on some providers, so it must never be touched implicitly
        handle1.Verify(h => h.HandleLostToken, Times.Never);
        handle2.Verify(h => h.HandleLostToken, Times.Never);
    }

    [Test]
    public void TestHandleLostTokenIsLazy()
    {
        var handle1 = new Mock<IDistributedSynchronizationHandle>();
        var handle2 = new Mock<IDistributedSynchronizationHandle>();
        var sut = AcquireAll(Succeeds(handle1), Succeeds(handle2));

        handle1.Verify(h => h.HandleLostToken, Times.Never);
        handle2.Verify(h => h.HandleLostToken, Times.Never);

        sut.HandleLostToken.ThrowIfCancellationRequested();

        handle1.Verify(h => h.HandleLostToken, Times.Once);
        handle2.Verify(h => h.HandleLostToken, Times.Once);

        // the token is cached; the underlying handles are not consulted again
        sut.HandleLostToken.ThrowIfCancellationRequested();
        handle1.Verify(h => h.HandleLostToken, Times.Once);
        handle2.Verify(h => h.HandleLostToken, Times.Once);

        sut.Dispose();
    }

    [Test]
    public void TestHandleLostTokenLinksUnderlyingTokens()
    {
        using var source = new CancellationTokenSource();
        var handle1 = new Mock<IDistributedSynchronizationHandle>();
        handle1.SetupGet(h => h.HandleLostToken).Returns(source.Token);
        var handle2 = new Mock<IDistributedSynchronizationHandle>();

        using var sut = AcquireAll(Succeeds(handle1), Succeeds(handle2));

        var token = sut.HandleLostToken;
        Assert.That(token.CanBeCanceled, Is.True);
        Assert.That(token.IsCancellationRequested, Is.False);
        source.Cancel();
        Assert.That(token.IsCancellationRequested, Is.True);
    }

    [Test]
    public void TestHandleLostTokenThrowsAfterDispose()
    {
        var sut = AcquireAll(
            Succeeds(new Mock<IDistributedSynchronizationHandle>()),
            Succeeds(new Mock<IDistributedSynchronizationHandle>()));
        sut.Dispose();

        Assert.Throws<ObjectDisposedException>(() => sut.HandleLostToken.ThrowIfCancellationRequested());
    }

    private static IDistributedSynchronizationHandle AcquireAll(params TestPrimitive[] primitives)
    {
        var result = CompositeDistributedSynchronizationHandle.TryAcquireAllAsync(
                primitives,
                static (p, t, c) => p.TryAcquireAsync(t, c),
                Timeout.InfiniteTimeSpan,
                CancellationToken.None)
            .AsTask()
            .GetAwaiter()
            .GetResult();
        var handle = result.GetHandleOrDefault();
        Assert.That(handle, Is.Not.Null);
        return handle!;
    }

    [Test]
    public async Task TestAcquiresSequentiallyAndStopsAtFirstFailure()
    {
        List<string> acquisitionOrder = [];
        var acquired = new Mock<IDistributedSynchronizationHandle>();

        TestPrimitive Recording(string name, bool succeeds) => new(
            name,
            (_, _) =>
            {
                acquisitionOrder.Add(name);
                return new(succeeds ? acquired.Object : null);
            });

        var result = await TryAcquireAllAsync(
            Timeout.InfiniteTimeSpan,
            CancellationToken.None,
            Recording("first", succeeds: true),
            Recording("fails", succeeds: false),
            Recording("never-attempted", succeeds: true));

        Assert.That(result.GetHandleOrDefault(), Is.Null);
        Assert.That(acquisitionOrder, Is.EqualTo(new[] { "first", "fails" }));
        acquired.Verify(h => h.DisposeAsync(), Times.Once);
    }

    [Test]
    public async Task TestTimeoutNamesTheFailedPrimitive()
    {
        var result = await TryAcquireAllAsync(
            TimeSpan.FromSeconds(30),
            CancellationToken.None,
            new TestPrimitive("succeeds", (_, _) => new(new Mock<IDistributedSynchronizationHandle>().Object)),
            new TestPrimitive("cannot-be-acquired", (_, _) => default));

        var exception = Assert.Throws<TimeoutException>(() => _ = result.Handle);
        Assert.That(exception!.Message, Does.Contain("cannot-be-acquired"));
    }

    [Test]
    public void TestAcquisitionExceptionPropagatesAndDisposesAcquiredHandles()
    {
        var acquired = new Mock<IDistributedSynchronizationHandle>();

        var task = TryAcquireAllAsync(
            Timeout.InfiniteTimeSpan,
            CancellationToken.None,
            new TestPrimitive("acquired", (_, _) => new(acquired.Object)),
            new TestPrimitive("throws", (_, _) => throw new InvalidOperationException("boom"))).AsTask();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => task);
        Assert.That(exception!.Message, Is.EqualTo("boom"));
        acquired.Verify(h => h.DisposeAsync(), Times.Once);
    }

    [Test]
    public async Task TestSinglePrimitiveUsesFastPath()
    {
        var handle = new Mock<IDistributedSynchronizationHandle>();
        var result = await TryAcquireAllAsync(
            Timeout.InfiniteTimeSpan,
            CancellationToken.None,
            new TestPrimitive("only", (_, _) => new(handle.Object)));

        // the single-primitive fast path returns the underlying handle rather than wrapping it
        Assert.That(result.GetHandleOrDefault(), Is.SameAs(handle.Object));
    }

    [Test]
    public void TestRequiresAtLeastOnePrimitive()
    {
        Assert.Throws<ArgumentException>(() => new ChainedDistributedLock("name", []));
        Assert.Throws<ArgumentNullException>(() => new ChainedDistributedLock("name", null!));
        Assert.Throws<ArgumentException>(
            () => new ChainedDistributedLock("name", [new Mock<IDistributedLock>().Object, null!]));
    }

    private static ValueTask<CompositeDistributedSynchronizationHandle.AcquireResult> TryAcquireAllAsync(
        TimeSpan? timeout,
        CancellationToken cancellationToken,
        params TestPrimitive[] primitives) =>
        CompositeDistributedSynchronizationHandle.TryAcquireAllAsync(
            primitives,
            static (p, t, c) => p.TryAcquireAsync(t, c),
            timeout,
            cancellationToken);

    internal static TestPrimitive Succeeds(Mock<IDistributedSynchronizationHandle> handle) =>
        new(handle.Object.GetHashCode().ToString(), (_, _) => new(handle.Object));

    internal sealed class TestPrimitive(
        string name,
        Func<TimeSpan, CancellationToken, ValueTask<IDistributedSynchronizationHandle?>> tryAcquireAsync)
    {
        public string Name => name;

        public ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
            tryAcquireAsync(timeout, cancellationToken);

        public override string ToString() => name;
    }
}
