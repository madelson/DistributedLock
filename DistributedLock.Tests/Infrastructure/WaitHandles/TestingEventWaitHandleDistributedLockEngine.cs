using Medallion.Threading.WaitHandles;

namespace Medallion.Threading.Tests.WaitHandles
{
    public sealed class TestingEventWaitHandleDistributedLockEngine : TestingDistributedLockEngine
    {
        internal override IDistributedLock CreateLockWithExactName(string name) => new EventWaitHandleDistributedLock(name);

        internal override string GetSafeName(string name) => EventWaitHandleDistributedLock.GetSafeName(name);

        internal override bool IsReentrant => false;
    }
}
