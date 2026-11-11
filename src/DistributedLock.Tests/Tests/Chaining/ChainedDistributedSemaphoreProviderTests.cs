using Medallion.Threading.Chaining;
using Moq;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Chaining;

[TestFixture]
public class ChainedDistributedSemaphoreProviderTests
{
    private const string Name = "SemaphoreTest";

    [Test]
    public async Task Acquire_AllProvidersSucceed_ReturnsHandle()
    {
        var (p1, h1) = CreateProvider(true);
        var (p2, h2) = CreateProvider(true);
        var sut = new ChainedDistributedSemaphoreProvider(p1.Object, p2.Object);

        await using (var handle = await sut.CreateSemaphore(Name, 1).AcquireAsync())
        {
            Assert.That(handle, Is.Not.Null);
        }

        h1.Value.Verify(x => x.DisposeAsync(), Times.Once);
        h2.Value.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Test]
    public void Acquire_PartialFailure_Throws()
    {
        var (p1, _) = CreateProvider(true);
        var (p2, _) = CreateProvider(false);
        var sut = new ChainedDistributedSemaphoreProvider(p1.Object, p2.Object);

        Assert.ThrowsAsync<TimeoutException>(async () =>
        {
            await sut.CreateSemaphore(Name, 1).AcquireAsync();
        });
    }

    [Test]
    public async Task TryAcquire_AllProvidersSucceed_ReturnsHandle()
    {
        var (p1, h1) = CreateProvider(true);
        var (p2, h2) = CreateProvider(true);
        var sut = new ChainedDistributedSemaphoreProvider(p1.Object, p2.Object);

        var handle = await sut.CreateSemaphore(Name, 1).TryAcquireAsync();
        Assert.That(handle, Is.Not.Null);

        if (handle != null)
        {
            await using (handle) { }
        }

        h1.Value.Verify(x => x.DisposeAsync(), Times.Once);
        h2.Value.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Test]
    public async Task TryAcquire_PartialFailure_ReturnsNull()
    {
        var (p1, _) = CreateProvider(true);
        var (p2, _) = CreateProvider(false);
        var sut = new ChainedDistributedSemaphoreProvider(p1.Object, p2.Object);

        var handle = await sut.CreateSemaphore(Name, 1).TryAcquireAsync();
        Assert.That(handle, Is.Null);
    }

    private static (Mock<IDistributedSemaphoreProvider> provider,
        Lazy<Mock<IDistributedSynchronizationHandle>> handle) CreateProvider(bool succeed)
    {
        var provider = new Mock<IDistributedSemaphoreProvider>();
        var semaphore = new Mock<IDistributedSemaphore>();
        var handle = new Lazy<Mock<IDistributedSynchronizationHandle>>(() =>
        {
            var h = new Mock<IDistributedSynchronizationHandle>();
            h.Setup(x => x.HandleLostToken).Returns(CancellationToken.None);
            h.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
            return h;
        });

        provider.Setup(p => p.CreateSemaphore(Name, It.IsAny<int>())).Returns(semaphore.Object);

        if (succeed)
        {
            semaphore.Setup(s => s.AcquireAsync(It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => handle.Value.Object);
            semaphore.Setup(s => s.TryAcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => handle.Value.Object);
            semaphore.Setup(s => s.Acquire(It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Returns(() => handle.Value.Object);
            semaphore.Setup(s => s.TryAcquire(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns(() => handle.Value.Object);
        }
        else
        {
            semaphore.Setup(s => s.AcquireAsync(It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException());
            semaphore.Setup(s => s.TryAcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IDistributedSynchronizationHandle?)null);
            semaphore.Setup(s => s.Acquire(It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());
            semaphore.Setup(s => s.TryAcquire(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns((IDistributedSynchronizationHandle?)null);
        }

        return (provider, handle);
    }
}