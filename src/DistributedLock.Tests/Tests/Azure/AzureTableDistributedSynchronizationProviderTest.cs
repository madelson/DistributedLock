using Azure.Data.Tables;
using Medallion.Threading.Azure;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Azure;

public class AzureTableDistributedSynchronizationProviderTest
{
    [Test]
    public void TestArgumentValidation()
    {
        Assert.Throws<ArgumentNullException>(() => new AzureTableDistributedSynchronizationProvider(null!));
    }

    [Test]
    public async Task BasicTest()
    {
        const string LockName = TargetFramework.Current + "TableProviderBasicTest";

        var table = new TableClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultTableName);
        var provider = new AzureTableDistributedSynchronizationProvider(table);
        await using (await provider.AcquireLockAsync(LockName))
        {
            await using var handle = await provider.TryAcquireLockAsync(LockName);
            Assert.That(handle, Is.Null);
        }
    }
}
