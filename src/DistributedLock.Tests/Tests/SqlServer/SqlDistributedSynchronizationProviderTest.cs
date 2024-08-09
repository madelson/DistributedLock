﻿using Medallion.Threading.SqlServer;
using NUnit.Framework;
using System.Data;

namespace Medallion.Threading.Tests.SqlServer;

public class SqlDistributedSynchronizationProviderTest
{
    [Test]
    public void TestArgumentValidation()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlDistributedSynchronizationProvider(default(string)!));
        Assert.Throws<ArgumentNullException>(() => new SqlDistributedSynchronizationProvider(default(IDbConnection)!));
        Assert.Throws<ArgumentNullException>(() => new SqlDistributedSynchronizationProvider(default(IDbTransaction)!));
    }

    [Test]
    public async Task BasicTest()
    {
        var provider = new SqlDistributedSynchronizationProvider(SqlServerSetUpFixture.SqlServer.GetConnectionString());

        const string LockName = TargetFramework.Current + "ProviderBasicTest";
        await using (await provider.AcquireLockAsync(LockName))
        {
            await using var handle = await provider.TryAcquireLockAsync(LockName);
            Assert.That(handle, Is.Null);
        }

        const string ReaderWriterLockName = TargetFramework.Current + "ProviderBasicTest_ReaderWriter";
        await using (await provider.AcquireReadLockAsync(ReaderWriterLockName))
        {
            await using var handle = await provider.TryAcquireWriteLockAsync(ReaderWriterLockName);
            Assert.That(handle, Is.Null);
        }

        const string SemaphoreName = TargetFramework.Current + "ProviderBasicTest_Semaphore";
        await using (await provider.AcquireSemaphoreAsync(SemaphoreName, 2))
        {
            await using var handle = await provider.TryAcquireSemaphoreAsync(SemaphoreName, 2);
            Assert.That(handle, Is.Not.Null);

            await using var failedHandle = await provider.TryAcquireSemaphoreAsync(SemaphoreName, 2);
            Assert.That(failedHandle, Is.Null);
        }
    }
}
