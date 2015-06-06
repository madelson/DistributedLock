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
        [TestMethod]
        public void TestGarbageCollection()
        {
            var @lock = this.CreateLock("gc_test");
            Func<WeakReference> abandonLock = () => new WeakReference(@lock.Acquire());

            var weakHandle = abandonLock();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            weakHandle.IsAlive.ShouldEqual(false);
            using (var handle = @lock.TryAcquire())
            {
                Assert.IsNotNull(handle);
            }
        }

        internal override IDistributedLock CreateLock(string name)
        {
            return new SystemDistributedLock(name);
        }
    }
}
