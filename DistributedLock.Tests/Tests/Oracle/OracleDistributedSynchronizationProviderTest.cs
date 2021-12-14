using Medallion.Threading.Oracle;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Oracle
{
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
                Assert.IsNull(handle);
            }

            await using (await provider.AcquireReadLockAsync(LockName))
            {
                await using var readHandle = await provider.TryAcquireReadLockAsync(LockName);
                Assert.IsNotNull(readHandle);

                await using (var upgradeHandle = await provider.TryAcquireUpgradeableReadLockAsync(LockName))
                {
                    Assert.IsNotNull(upgradeHandle);
                    Assert.IsFalse(await upgradeHandle!.TryUpgradeToWriteLockAsync());
                }

                await using var writeHandle = await provider.TryAcquireWriteLockAsync(LockName);
                Assert.IsNull(writeHandle);
            }
        }
    }
}
