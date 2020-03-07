using Medallion.Threading.WaitHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests
{
    public sealed class TestingEventWaitHandleDistributedLockEngine : TestingDistributedLockEngine
    {
        internal override IDistributedLock CreateLockWithExactName(string name) => new EventWaitHandleDistributedLock(name);

        internal override string GetSafeName(string name) => EventWaitHandleDistributedLock.GetSafeName(name);

        internal override bool IsReentrant => false;
    }
}
