using Medallion.Threading.WaitHandles;
using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests.WaitHandles
{
    [SupportsContinuousIntegration]
    public sealed class TestingEventWaitHandleDistributedLockProvider : TestingLockProvider<TestingWaitHandlesSynchronizationStrategy>
    {
        public override IDistributedLock CreateLockWithExactName(string name) => new EventWaitHandleDistributedLock(name, exactName: true);

        public override string GetSafeName(string name) => EventWaitHandleDistributedLock.GetSafeName(name);
    }

    [SupportsContinuousIntegration]
    public sealed class TestingWaitHandleDistributedSemaphoreProvider : TestingSemaphoreProvider<TestingWaitHandlesSynchronizationStrategy>
    {
        public override IDistributedSemaphore CreateSemaphoreWithExactName(string name, int maxCount) => new WaitHandleDistributedSemaphore(name, maxCount, exactName: true);

        public override string GetSafeName(string name) => WaitHandleDistributedSemaphore.GetSafeName(name);
    }
}
