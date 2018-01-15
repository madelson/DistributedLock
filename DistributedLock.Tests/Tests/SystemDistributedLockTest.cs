using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests
{
    [TestClass]
    public class SystemDistributedLockTest : TestBase
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

        [TestMethod]
        public void TestGetSafeLockNameCompat()
        {
            SystemDistributedLock.GetSafeLockName("").ShouldEqual("EMPTYz4PhNX7vuL3xVChQ1m2AB9Yg5AULVxXcg/SpIdNs6c5H0NE8XYXysP+DGNKHfuwvY7kxvUdBeoGlODJ6+SfaPg==");
            SystemDistributedLock.GetSafeLockName("abc").ShouldEqual("abc");
            SystemDistributedLock.GetSafeLockName("\\").ShouldEqual("_CgzRFsLFf7El/ZraEx9sqWRYeplYohSBSmI9sYIe1c4y2u7ECFoU4x2QCjV7HiVJMZsuDMLIz7r8akpKr+viAw==");
            SystemDistributedLock.GetSafeLockName(new string('a', SystemDistributedLock.MaxLockNameLength)).ShouldEqual(new string('a', SystemDistributedLock.MaxLockNameLength));
            SystemDistributedLock.GetSafeLockName(new string('\\', SystemDistributedLock.MaxLockNameLength)).ShouldEqual("_____________________________________________________________________________________________________________________________________________________________________Y7DJXlpJeJjeX5XAOWV+ka/3ONBj5dHhKWcSH4pd5AC9YHFm+l1gBArGpBSBn3WcX00ArcDtKw7g24kJaHLifQ==");
            SystemDistributedLock.GetSafeLockName(new string('x', SystemDistributedLock.MaxLockNameLength + 1)).ShouldEqual("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxsrCnXZ1XHiT//dOSBfAU0iC4Gtnlr0dQACBUK8Ev2OdRYJ9jcvbiqVCv/rjyPemTW9AvOonkdr0B2bG04gmeYA==");
        }

        internal SystemDistributedLock CreateLock(string name) => new SystemDistributedLock(name, abandonmentCheckFrequency: TimeSpan.FromSeconds(.3));
    }
}
