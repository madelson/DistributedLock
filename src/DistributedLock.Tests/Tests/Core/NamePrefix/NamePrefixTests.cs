using Moq;
using Medallion.Threading.NamePrefix;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Tests.Core.NamePrefix;

internal class NamePrefixTests
{
    [TestCase("prefix1", "prefix1")]
    [TestCase("prefix1,prefix2", "prefix1prefix2")]
    [TestCase("prefix1,prefix2,prefix3", "prefix1prefix2prefix3")]
    public void CreateLock_ShouldPrefixKey(string prefixes, string expectedPrefix)
    {
        var rootProvider = new Mock<IDistributedLockProvider>(MockBehavior.Strict);
        rootProvider
            .Setup(p => p.CreateLock(It.IsAny<string>()))
            .Returns(Mock.Of<IDistributedLock>());

        IDistributedLockProvider sut = rootProvider.Object;
        var intermediateProviders = new List<IDistributedLockProvider>();
        foreach (var prefix in prefixes.Split(','))
        {
            if (sut != rootProvider.Object)
            {
                intermediateProviders.Add(sut);
            }
            sut = sut.WithNamePrefix(prefix);
        }

        _ = sut.CreateLock("my-lock");

        // The root provider is called exactly once, with the concatenated prefix
        rootProvider.Verify(p => p.CreateLock($"{expectedPrefix}my-lock"), Times.Once);
        rootProvider.VerifyNoOtherCalls();

        // The final provider wraps the root directly, so no intermediate provider takes part in the call
        var prefixed = sut as NamePrefixedDistributedLockProvider;
        Assert.That(prefixed, Is.Not.Null);
        Assert.That(prefixed!.Inner, Is.SameAs(rootProvider.Object));
        Assert.That(prefixed.NamePrefix, Is.EqualTo(expectedPrefix));
        foreach (var intermediate in intermediateProviders)
        {
            Assert.That(prefixed.Inner, Is.Not.SameAs(intermediate));
        }
    }

    [TestCase("prefix1", "prefix1")]
    [TestCase("prefix1,prefix2", "prefix1prefix2")]
    [TestCase("prefix1,prefix2,prefix3", "prefix1prefix2prefix3")]
    public void CreateReaderWriterLock_ShouldPrefixKey(string prefixes, string expectedPrefix)
    {
        var rootProvider = new Mock<IDistributedReaderWriterLockProvider>(MockBehavior.Strict);
        rootProvider
            .Setup(p => p.CreateReaderWriterLock(It.IsAny<string>()))
            .Returns(Mock.Of<IDistributedReaderWriterLock>());

        IDistributedReaderWriterLockProvider sut = rootProvider.Object;
        var intermediateProviders = new List<IDistributedReaderWriterLockProvider>();
        foreach (var prefix in prefixes.Split(','))
        {
            if (sut != rootProvider.Object)
            {
                intermediateProviders.Add(sut);
            }
            sut = sut.WithNamePrefix(prefix);
        }

        _ = sut.CreateReaderWriterLock("my-lock");

        // The root provider is called exactly once, with the concatenated prefix
        rootProvider.Verify(p => p.CreateReaderWriterLock($"{expectedPrefix}my-lock"), Times.Once);
        rootProvider.VerifyNoOtherCalls();

        // The final provider wraps the root directly, so no intermediate provider takes part in the call
        var prefixed = sut as NamePrefixedDistributedReaderWriterLockProvider;
        Assert.That(prefixed, Is.Not.Null);
        Assert.That(prefixed!.Inner, Is.SameAs(rootProvider.Object));
        Assert.That(prefixed.NamePrefix, Is.EqualTo(expectedPrefix));
        foreach (var intermediate in intermediateProviders)
        {
            Assert.That(prefixed.Inner, Is.Not.SameAs(intermediate));
        }
    }

    [TestCase("prefix1", "prefix1")]
    [TestCase("prefix1,prefix2", "prefix1prefix2")]
    [TestCase("prefix1,prefix2,prefix3", "prefix1prefix2prefix3")]
    public void CreateUpgradeableReaderWriterLock_ShouldPrefixKey(string prefixes, string expectedPrefix)
    {
        var rootProvider = new Mock<IDistributedUpgradeableReaderWriterLockProvider>(MockBehavior.Strict);
        rootProvider
            .Setup(p => p.CreateUpgradeableReaderWriterLock(It.IsAny<string>()))
            .Returns(Mock.Of<IDistributedUpgradeableReaderWriterLock>());

        IDistributedUpgradeableReaderWriterLockProvider sut = rootProvider.Object;
        var intermediateProviders = new List<IDistributedUpgradeableReaderWriterLockProvider>();
        foreach (var prefix in prefixes.Split(','))
        {
            if (sut != rootProvider.Object)
            {
                intermediateProviders.Add(sut);
            }
            sut = sut.WithNamePrefix(prefix);
        }

        _ = sut.CreateUpgradeableReaderWriterLock("my-lock");

        // The root provider is called exactly once, with the concatenated prefix
        rootProvider.Verify(p => p.CreateUpgradeableReaderWriterLock($"{expectedPrefix}my-lock"), Times.Once);
        rootProvider.VerifyNoOtherCalls();

        // The final provider wraps the root directly, so no intermediate provider takes part in the call
        var prefixed = sut as NamePrefixedDistributedUpgradeableReaderWriterLockProvider;
        Assert.That(prefixed, Is.Not.Null);
        Assert.That(prefixed!.Inner, Is.SameAs(rootProvider.Object));
        Assert.That(prefixed.NamePrefix, Is.EqualTo(expectedPrefix));
        foreach (var intermediate in intermediateProviders)
        {
            Assert.That(prefixed.Inner, Is.Not.SameAs(intermediate));
        }
    }

    [TestCase("prefix1", "prefix1")]
    [TestCase("prefix1,prefix2", "prefix1prefix2")]
    [TestCase("prefix1,prefix2,prefix3", "prefix1prefix2prefix3")]
    public void CreateSemaphore_ShouldPrefixKey(string prefixes, string expectedPrefix)
    {
        var rootProvider = new Mock<IDistributedSemaphoreProvider>(MockBehavior.Strict);
        rootProvider
            .Setup(p => p.CreateSemaphore(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(Mock.Of<IDistributedSemaphore>());

        IDistributedSemaphoreProvider sut = rootProvider.Object;
        var intermediateProviders = new List<IDistributedSemaphoreProvider>();
        foreach (var prefix in prefixes.Split(','))
        {
            if (sut != rootProvider.Object)
            {
                intermediateProviders.Add(sut);
            }
            sut = sut.WithNamePrefix(prefix);
        }

        _ = sut.CreateSemaphore("my-semaphore", 3);

        // The root provider is called exactly once, with the concatenated prefix
        rootProvider.Verify(p => p.CreateSemaphore($"{expectedPrefix}my-semaphore", 3), Times.Once);
        rootProvider.VerifyNoOtherCalls();

        // The final provider wraps the root directly, so no intermediate provider takes part in the call
        var prefixed = sut as NamePrefixedDistributedSemaphoreProvider;
        Assert.That(prefixed, Is.Not.Null);
        Assert.That(prefixed!.Inner, Is.SameAs(rootProvider.Object));
        Assert.That(prefixed.NamePrefix, Is.EqualTo(expectedPrefix));
        foreach (var intermediate in intermediateProviders)
        {
            Assert.That(prefixed.Inner, Is.Not.SameAs(intermediate));
        }
    }
}