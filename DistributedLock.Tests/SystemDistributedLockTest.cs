using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests
{
    [TestClass]
    public class SystemDistributedLockTest : DistributedLockTestBase
    {
        internal override IDistributedLock CreateLock(string name)
        {
            return new SystemDistributedLock(name);
        }
    }
}
