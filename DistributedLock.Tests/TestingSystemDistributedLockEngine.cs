using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests
{
    public sealed class TestingSystemDistributedLockEngine : TestingDistributedLockEngine
    {
        internal override IDistributedLock CreateLockWithExactName(string name) => new SystemDistributedLock(name);

        internal override string GetSafeLockName(string name) => SystemDistributedLock.GetSafeLockName(name);

        internal override bool IsReentrant => false;
    }
}
