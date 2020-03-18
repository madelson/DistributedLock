using Medallion.Threading.WaitHandles;
using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests.WaitHandles
{
    public sealed class TestingEventWaitHandleLockProvider : ITestingLockProvider
    {
        public string CrossProcessLockType => nameof(EventWaitHandleDistributedLock);

        public IDistributedLock CreateLockWithExactName(string name) => new EventWaitHandleDistributedLock(name, exactName: true);

        public string GetSafeName(string name) => EventWaitHandleDistributedLock.GetSafeName(name);

        public void PerformAdditionalCleanupForHandleAbandonment() { }
        public void Dispose() { }
    }
}
