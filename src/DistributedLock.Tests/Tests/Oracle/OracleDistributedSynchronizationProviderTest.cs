using Medallion.Threading.Oracle;
using NUnit.Framework;
using System.Data;

namespace Medallion.Threading.Tests.Oracle;

public class OracleDistributedSynchronizationProviderTest
{
    [Test]
    public void TestArgumentValidation()
    {
        Assert.Throws<ArgumentNullException>(() => new OracleDistributedSynchronizationProvider(default(string)!));
        Assert.Throws<ArgumentNullException>(() => new OracleDistributedSynchronizationProvider(default(IDbConnection)!));
    }

    [Test]
    public async Task BasicTest()
    {
        var provider = new OracleDistributedSynchronizationProvider(TestingOracleDb.DefaultConnectionString);

        const string LockName = TargetFramework.Current + "ProviderBasicTest";

        await using (await provider.AcquireLockAsync(LockName))
        {
            await using var handle = await provider.TryAcquireLockAsync(LockName);
            Assert.That(handle, Is.Null);
        }

        await using (await provider.AcquireReadLockAsync(LockName))
        {
            await using var readHandle = await provider.TryAcquireReadLockAsync(LockName);
            Assert.That(readHandle, Is.Not.Null);

            await using (var upgradeHandle = await provider.TryAcquireUpgradeableReadLockAsync(LockName))
            {
                Assert.That(upgradeHandle, Is.Not.Null);
                Assert.That(await upgradeHandle!.TryUpgradeToWriteLockAsync(), Is.False);
            }

            await using var writeHandle = await provider.TryAcquireWriteLockAsync(LockName);
            Assert.That(writeHandle, Is.Null);
        }
    }
}
