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
}
