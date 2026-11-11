using Medallion.Threading.Chaining;
using Moq;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Chaining;

[TestFixture]
public class ChainedDistributedReaderWriterLockProviderTests
{
    private const string Name = "RwLockTest";

    [Test]
    public async Task AcquireRead_AllProvidersSucceed_ReturnsHandle()
    {
        var (p1, h1) = CreateProvider(true);
        var (p2, h2) = CreateProvider(true);
        var sut = new ChainedDistributedReaderWriterLockProvider(p1.Object, p2.Object);

        await using (var handle = await sut.CreateReaderWriterLock(Name).AcquireReadLockAsync())
        {
            Assert.That(handle, Is.Not.Null);
        }

        h1.Value.Verify(x => x.DisposeAsync(), Times.Once);
        h2.Value.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Test]
    public void AcquireRead_PartialFailure_Throws()
    {
        var (p1, _) = CreateProvider(true);
        var (p2, _) = CreateProvider(false);
        var sut = new ChainedDistributedReaderWriterLockProvider(p1.Object, p2.Object);

        Assert.ThrowsAsync<TimeoutException>(async () =>
        {
            await sut.CreateReaderWriterLock(Name).AcquireReadLockAsync();
        });
    }

    [Test]
    public async Task TryAcquireRead_AllProvidersSucceed_ReturnsHandle()
    {
        var (p1, h1) = CreateProvider(true);
        var (p2, h2) = CreateProvider(true);
        var sut = new ChainedDistributedReaderWriterLockProvider(p1.Object, p2.Object);

        var handle = await sut.CreateReaderWriterLock(Name).TryAcquireReadLockAsync();
        Assert.That(handle, Is.Not.Null);
        if (handle != null) await using (handle) { }

        h1.Value.Verify(x => x.DisposeAsync(), Times.Once);
        h2.Value.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Test]
    public async Task TryAcquireRead_PartialFailure_ReturnsNull()
    {
        var (p1, _) = CreateProvider(true);
        var (p2, _) = CreateProvider(false);
        var sut = new ChainedDistributedReaderWriterLockProvider(p1.Object, p2.Object);

        var handle = await sut.CreateReaderWriterLock(Name).TryAcquireReadLockAsync();
        Assert.That(handle, Is.Null);
    }

    [Test]
    public async Task AcquireWrite_AllProvidersSucceed_ReturnsHandle()
    {
        var (p1, h1) = CreateProvider(true);
        var (p2, h2) = CreateProvider(true);
        var sut = new ChainedDistributedReaderWriterLockProvider(p1.Object, p2.Object);

        await using (var handle = await sut.CreateReaderWriterLock(Name).AcquireWriteLockAsync())
        {
            Assert.That(handle, Is.Not.Null);
        }

        h1.Value.Verify(x => x.DisposeAsync(), Times.Once);
        h2.Value.Verify(x => x.DisposeAsync(), Times.Once);
    }

    private static (Mock<IDistributedReaderWriterLockProvider> provider,
        Lazy<Mock<IDistributedSynchronizationHandle>> handle) CreateProvider(bool succeed)
    {
        var provider = new Mock<IDistributedReaderWriterLockProvider>();
        var rwLock = new Mock<IDistributedReaderWriterLock>();
        var handle = new Lazy<Mock<IDistributedSynchronizationHandle>>(() =>
        {
            var h = new Mock<IDistributedSynchronizationHandle>();
            h.Setup(x => x.HandleLostToken).Returns(CancellationToken.None);
            h.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
            return h;
        });

        provider.Setup(p => p.CreateReaderWriterLock(Name)).Returns(rwLock.Object);

        if (succeed)
        {
            rwLock.Setup(l => l.AcquireReadLockAsync(It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => handle.Value.Object);
            rwLock.Setup(l => l.TryAcquireReadLockAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => handle.Value.Object);
            rwLock.Setup(l => l.AcquireWriteLockAsync(It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => handle.Value.Object);
            rwLock.Setup(l => l.TryAcquireWriteLockAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => handle.Value.Object);
            rwLock.Setup(l => l.AcquireReadLock(It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Returns(() => handle.Value.Object);
            rwLock.Setup(l => l.TryAcquireReadLock(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns(() => handle.Value.Object);
            rwLock.Setup(l => l.AcquireWriteLock(It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Returns(() => handle.Value.Object);
            rwLock.Setup(l => l.TryAcquireWriteLock(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns(() => handle.Value.Object);
        }
        else
        {
            rwLock.Setup(l => l.AcquireReadLockAsync(It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException());
            rwLock.Setup(l => l.TryAcquireReadLockAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IDistributedSynchronizationHandle?)null);
            rwLock.Setup(l => l.AcquireWriteLockAsync(It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TimeoutException());
            rwLock.Setup(l => l.TryAcquireWriteLockAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IDistributedSynchronizationHandle?)null);
            rwLock.Setup(l => l.AcquireReadLock(It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());
            rwLock.Setup(l => l.TryAcquireReadLock(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns((IDistributedSynchronizationHandle?)null);
            rwLock.Setup(l => l.AcquireWriteLock(It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException());
            rwLock.Setup(l => l.TryAcquireWriteLock(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns((IDistributedSynchronizationHandle?)null);
        }

        return (provider, handle);
    }
}