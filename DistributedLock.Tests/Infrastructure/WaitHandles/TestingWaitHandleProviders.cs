using Medallion.Threading.WaitHandles;
using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests.WaitHandles
{
    [SupportsContinuousIntegration(WindowsOnly = true)]
    public sealed class TestingEventWaitHandleDistributedLockProvider : TestingLockProvider<TestingWaitHandleSynchronizationStrategy>
    {
        public override IDistributedLock CreateLockWithExactName(string name) => new EventWaitHandleDistributedLock(name, exactName: true);

        public override string GetSafeName(string name) => EventWaitHandleDistributedLock.GetSafeName(name);
    }

    [SupportsContinuousIntegration(WindowsOnly = true)]
    public sealed class TestingWaitHandleDistributedSemaphoreProvider : TestingSemaphoreProvider<TestingWaitHandleSynchronizationStrategy>
    {
        public override IDistributedSemaphore CreateSemaphoreWithExactName(string name, int maxCount) => new WaitHandleDistributedSemaphore(name, maxCount, exactName: true);

        public override string GetSafeName(string name) => WaitHandleDistributedSemaphore.GetSafeName(name);
    }
}
