using Medallion.Threading.Postgres;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Postgres
{
    public class PostgresDistributedSynchronizationProviderTest
    {
        [Test]
        public void TestArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>(() => new PostgresDistributedSynchronizationProvider(default(string)!));
            Assert.Throws<ArgumentNullException>(() => new PostgresDistributedSynchronizationProvider(default(IDbConnection)!));
        }

        [Test]
        public async Task BasicTest()
        {
            var provider = new PostgresDistributedSynchronizationProvider(TestingPostgresDb.ConnectionString);

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
        }
    }
}
