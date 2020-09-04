using Medallion.Threading.Internal;
using Medallion.Threading.WaitHandles;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Tests.WaitHandles
{
    [Category("CI")]
    public class WaitHandleDistributedSemaphoreTest
    {
        [TestCase(null, NameStyle.Exact, ExpectedResult = typeof(ArgumentNullException))]
        [TestCase(null, NameStyle.Safe, ExpectedResult = typeof(ArgumentNullException))]
        [TestCase("abc", NameStyle.Exact, ExpectedResult = typeof(FormatException))]
        [TestCase(@"gLoBaL\weirdPrefixCasing", NameStyle.Exact, ExpectedResult = typeof(FormatException))]
        [TestCase(@"global\weirdPrefixCasing2", NameStyle.Exact, ExpectedResult = typeof(FormatException))]
        [TestCase("", NameStyle.AddPrefix, ExpectedResult = typeof(FormatException))]
        [TestCase(@"a\b", NameStyle.AddPrefix, ExpectedResult = typeof(FormatException))]
        public Type TestBadName(string? name, NameStyle nameStyle)
        {
            if (name != null)
            {
                this.TestWorkingName(name, NameStyle.Safe); // should always work
            }

            return Assert.Catch(() => CreateAsLock(name!, nameStyle)).GetType();
        }

        [TestCase(" \t", NameStyle.AddPrefix)]
        [TestCase("/a/b/c", NameStyle.AddPrefix)]
        [TestCase("\r\n", NameStyle.AddPrefix)]
        public void TestWorkingName(string name, NameStyle nameStyle) =>
            Assert.DoesNotThrow(() => CreateAsLock(name, nameStyle).Acquire().Dispose());

        [Test]
        public void TestMaxLengthNames()
        {
            WaitHandleDistributedSemaphore.MaxNameLength.ShouldEqual(DistributedWaitHandleHelpers.MaxNameLength);

            var maxLengthName = DistributedWaitHandleHelpers.GlobalPrefix
                + new string('a', WaitHandleDistributedSemaphore.MaxNameLength - DistributedWaitHandleHelpers.GlobalPrefix.Length);
            this.TestWorkingName(maxLengthName, NameStyle.Exact);
            this.TestBadName(maxLengthName + "a", NameStyle.Exact);
        }

        [Test]
        public async Task TestGarbageCollection()
        {
            var @lock = CreateAsLock("gc_test", NameStyle.AddPrefix);
            WeakReference AbandonLock() => new WeakReference(@lock.Acquire());

            var weakHandle = AbandonLock();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await ManagedFinalizerQueue.Instance.FinalizeAsync();

            weakHandle.IsAlive.ShouldEqual(false);
            using var handle = @lock.TryAcquire();
            Assert.IsNotNull(handle);
        }

        [Test]
        public void TestGetSafeLockNameCompat()
        {
            // stored separately for testing compat
            const int MaxNameLengthWithoutGlobalPrefix = 253;
            (DistributedWaitHandleHelpers.MaxNameLength - DistributedWaitHandleHelpers.GlobalPrefix.Length)
                .ShouldEqual(MaxNameLengthWithoutGlobalPrefix);

            WaitHandleDistributedSemaphore.GetSafeName("").ShouldEqual(@"Global\EMPTYz4PhNX7vuL3xVChQ1m2AB9Yg5AULVxXcg/SpIdNs6c5H0NE8XYXysP+DGNKHfuwvY7kxvUdBeoGlODJ6+SfaPg==");
            WaitHandleDistributedSemaphore.GetSafeName("abc").ShouldEqual(@"Global\abc");
            WaitHandleDistributedSemaphore.GetSafeName("\\").ShouldEqual(@"Global\_CgzRFsLFf7El/ZraEx9sqWRYeplYohSBSmI9sYIe1c4y2u7ECFoU4x2QCjV7HiVJMZsuDMLIz7r8akpKr+viAw==");
            WaitHandleDistributedSemaphore.GetSafeName(new string('a', MaxNameLengthWithoutGlobalPrefix))
                .ShouldEqual(@"Global\" + new string('a', MaxNameLengthWithoutGlobalPrefix));
            WaitHandleDistributedSemaphore.GetSafeName(new string('\\', MaxNameLengthWithoutGlobalPrefix))
                .ShouldEqual(@"Global\_____________________________________________________________________________________________________________________________________________________________________Y7DJXlpJeJjeX5XAOWV+ka/3ONBj5dHhKWcSH4pd5AC9YHFm+l1gBArGpBSBn3WcX00ArcDtKw7g24kJaHLifQ==");
            WaitHandleDistributedSemaphore.GetSafeName(new string('x', MaxNameLengthWithoutGlobalPrefix + 1))
                .ShouldEqual(@"Global\xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxsrCnXZ1XHiT//dOSBfAU0iC4Gtnlr0dQACBUK8Ev2OdRYJ9jcvbiqVCv/rjyPemTW9AvOonkdr0B2bG04gmeYA==");
        }

        private static WaitHandleDistributedSemaphore CreateAsLock(string name, NameStyle nameStyle) =>
            new WaitHandleDistributedSemaphore(
                (nameStyle == NameStyle.AddPrefix ? DistributedWaitHandleHelpers.GlobalPrefix + name : name),
                maxCount: 1,
                abandonmentCheckCadence: TimeSpan.FromSeconds(.3),
                exactName: nameStyle != NameStyle.Safe
            );

        public enum NameStyle
        {
            Exact,
            AddPrefix,
            Safe,
        }
    }
}
