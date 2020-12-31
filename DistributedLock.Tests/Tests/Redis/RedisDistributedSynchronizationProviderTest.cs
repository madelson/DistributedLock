using Medallion.Threading.Redis;
using NUnit.Framework;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Redis
{
    public class RedisDistributedSynchronizationProviderTest
    {
        [Test]
        public void TestArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>(() => new RedisDistributedSynchronizationProvider(default(IDatabase)!));
            Assert.Throws<ArgumentNullException>(() => new RedisDistributedSynchronizationProvider(default(IEnumerable<IDatabase>)!));
            Assert.Throws<ArgumentNullException>(() => new RedisDistributedSynchronizationProvider(new[] { default(IDatabase)! }));
            Assert.Throws<ArgumentException>(() => new RedisDistributedSynchronizationProvider(Array.Empty<IDatabase>()));
        }

        [Test]
        public async Task BasicTest()
        {
            var provider = new RedisDistributedSynchronizationProvider(RedisServer.GetDefaultServer(0).Multiplexer.GetDatabase());

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
