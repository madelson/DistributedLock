using Medallion.Threading.WaitHandles;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.WaitHandles;

public class WaitHandleDistributedSynchronizationProviderTest
{
    [Test]
    public async Task BasicTest()
    {
        var provider = new WaitHandleDistributedSynchronizationProvider();

        const string LockName = TargetFramework.Current + "ProviderBasicTest";
        await using (await provider.AcquireLockAsync(LockName))
        {
            await using var handle = await provider.TryAcquireLockAsync(LockName);
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
