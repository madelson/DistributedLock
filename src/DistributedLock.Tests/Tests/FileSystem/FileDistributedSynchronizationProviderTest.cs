using Medallion.Threading.FileSystem;
using NUnit.Framework;

namespace Medallion.Threading.Tests.FileSystem;

public class FileDistributedSynchronizationProviderTest
{
    private static readonly string LockFileDirectory = Path.Combine(Path.GetTempPath(), nameof(FileDistributedSynchronizationProviderTest), TargetFramework.Current);
    private static DirectoryInfo LockFileDirectoryInfo => new(LockFileDirectory);

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        if (Directory.Exists(LockFileDirectory))
        {
            Directory.Delete(LockFileDirectory, recursive: true);
        }
    }

    [Test]
    public void TestArgumentValidation()
    {
        Assert.Throws<ArgumentNullException>(() => new FileDistributedSynchronizationProvider(null!));
    }

    [Test]
    public async Task BasicTest()
    {
        var provider = new FileDistributedSynchronizationProvider(LockFileDirectoryInfo);
        await using (await provider.AcquireLockAsync("ProviderBasicTest"))
        {
            await using var handle = await provider.TryAcquireLockAsync("ProviderBasicTest");
            Assert.That(handle, Is.Null);
        }
    }
}
