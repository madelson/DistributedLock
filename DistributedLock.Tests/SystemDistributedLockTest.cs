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
        public void TestBadConstructorArguments()
        {
            TestHelper.AssertThrows<ArgumentNullException>(() => this.CreateLock(null));
            TestHelper.AssertThrows<ArgumentNullException>(() => this.CreateLock(""));
            TestHelper.AssertThrows<FormatException>(() => this.CreateLock(new string('a', SystemDistributedLock.MaxLockNameLength + 1)));
            TestHelper.AssertThrows<FormatException>(() => this.CreateLock(@"a\b"));

            // weird but valid args
            TestHelper.AssertDoesNotThrow(() => this.CreateLock(new string('a', SystemDistributedLock.MaxLockNameLength)).Acquire().Dispose());
            TestHelper.AssertDoesNotThrow(() => this.CreateLock(" \t").Acquire().Dispose());
            TestHelper.AssertDoesNotThrow(() => this.CreateLock("/a/b/c").Acquire().Dispose());
        }

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
