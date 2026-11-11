using Medallion.Threading.Chaining;
using Moq;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Chaining;

[TestFixture]
public class ChainedDistributedLockProviderTests
{
    private const string Name = "LockTest";

    [Test]
    public async Task Acquire_AllProvidersSucceed_ReturnsHandle()
    {
        var (p1, h1) = CreateProvider(true);
        var (p2, h2) = CreateProvider(true);
        var sut = new ChainedDistributedLockProvider(p1.Object, p2.Object);

        await using (var handle = await sut.CreateLock(Name).AcquireAsync())
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
        var sut = new ChainedDistributedLockProvider(p1.Object, p2.Object);

        Assert.ThrowsAsync<TimeoutException>(async () =>
        {
            await sut.CreateLock(Name).AcquireAsync();
        });
    }

    [Test]
    public async Task TryAcquire_AllProvidersSucceed_ReturnsHandle()
    {
        var (p1, h1) = CreateProvider(true);
        var (p2, h2) = CreateProvider(true);
        var sut = new ChainedDistributedLockProvider(p1.Object, p2.Object);

        await using (var handle = await sut.CreateLock(Name).TryAcquireAsync())
        {
            Assert.That(handle, Is.Not.Null);
        }
        h1.Value.Verify(x => x.DisposeAsync(), Times.Once);
        h2.Value.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Test]
    public async Task TryAcquire_PartialFailure_ReturnsNull()
    {
        var (p1, _) = CreateProvider(true);
        var (p2, _) = CreateProvider(false);
        var sut = new ChainedDistributedLockProvider(p1.Object, p2.Object);

        var handle = await sut.CreateLock(Name).TryAcquireAsync();
        Assert.That(handle, Is.Null);
    }

    private static (Mock<IDistributedLockProvider> provider,
        Lazy<Mock<IDistributedSynchronizationHandle>> handle) CreateProvider(bool succeed)
    {
        var provider = new Mock<IDistributedLockProvider>();
        var lockMock = new Mock<IDistributedLock>();
        var handle = new Lazy<Mock<IDistributedSynchronizationHandle>>(() =>
        {
            var h = new Mock<IDistributedSynchronizationHandle>();
            h.Setup(x => x.HandleLostToken).Returns(CancellationToken.None);
            h.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
            return h;
        });

        provider.Setup(p => p.CreateLock(Name)).Returns(lockMock.Object);

        if (succeed)
        {
            lockMock.Setup(l => l.AcquireAsync(It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => handle.Value.Object);
            lockMock.Setup(l => l.TryAcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => handle.Value.Object);
            lockMock.Setup(l => l.Acquire(It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Returns(() => handle.Value.Object);
            lockMock.Setup(l => l.TryAcquire(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns(() => handle.Value.Object);
        }
        else
        {
            lockMock.Setup(l => l.AcquireAsync(It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException());
            lockMock.Setup(l => l.TryAcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IDistributedSynchronizationHandle?)null);
            lockMock.Setup(l => l.Acquire(It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());
            lockMock.Setup(l => l.TryAcquire(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns((IDistributedSynchronizationHandle?)null);
        }

        return (provider, handle);
    }
}