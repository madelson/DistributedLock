﻿using Medallion.Threading.Postgres;
using NUnit.Framework;
using System.Data;
#if NET7_0_OR_GREATER
using System.Data.Common;
#endif

namespace Medallion.Threading.Tests.Postgres;

public class PostgresDistributedSynchronizationProviderTest
{
    [Test]
    public void TestArgumentValidation()
    {
        Assert.Throws<ArgumentNullException>(() => new PostgresDistributedSynchronizationProvider(default(string)!));
        Assert.Throws<ArgumentNullException>(() => new PostgresDistributedSynchronizationProvider(default(IDbConnection)!));
#if NET7_0_OR_GREATER
        Assert.Throws<ArgumentNullException>(() => new PostgresDistributedSynchronizationProvider(default(DbDataSource)!));
#endif
    }

    [Test]
    public async Task BasicTest()
    {
        var provider = new PostgresDistributedSynchronizationProvider(PostgresSetUpFixture.PostgreSql.GetConnectionString());

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
    }
}