using Medallion.Threading.SqlServer;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.SqlServer
{
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
            var provider = new SqlDistributedSynchronizationProvider(TestingSqlServerDb.DefaultConnectionString);

            const string LockName = TargetFramework.Current + "ProviderBasicTest";
            await using (await provider.AcquireLockAsync(LockName))
            {
                await using var handle = await provider.TryAcquireLockAsync(LockName);
                Assert.IsNull(handle);
            }

            const string ReaderWriterLockName = TargetFramework.Current + "ProviderBasicTest_ReaderWriter";
            await using (await provider.AcquireReadLockAsync(ReaderWriterLockName))
            {
                await using var handle = await provider.TryAcquireWriteLockAsync(ReaderWriterLockName);
                Assert.IsNull(handle);
            }

            const string SemaphoreName = TargetFramework.Current + "ProviderBasicTest_Semaphore";
            await using (await provider.AcquireSemaphoreAsync(SemaphoreName, 2))
            {
                await using var handle = await provider.TryAcquireSemaphoreAsync(SemaphoreName, 2);
                Assert.IsNotNull(handle);

                await using var failedHandle = await provider.TryAcquireSemaphoreAsync(SemaphoreName, 2);
                Assert.IsNull(failedHandle);
            }
        }
    }
}
