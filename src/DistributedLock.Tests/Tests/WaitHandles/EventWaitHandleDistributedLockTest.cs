using Medallion.Threading.WaitHandles;
using NUnit.Framework;

namespace Medallion.Threading.Tests.WaitHandles;

[Category("CIWindows")]
public class EventWaitHandleDistributedLockTest
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

        return Assert.Catch(() => CreateLock(name!, nameStyle))!.GetType();
    }

    [TestCase(" \t", NameStyle.AddPrefix)]
    [TestCase("/a/b/c", NameStyle.AddPrefix)]
    [TestCase("\r\n", NameStyle.AddPrefix)]
    public void TestWorkingName(string name, NameStyle nameStyle) =>
        Assert.DoesNotThrow(() => CreateLock(name, nameStyle).Acquire().Dispose());

    [Test]
    public void TestMaxLengthNames()
    {
        var maxLengthName = DistributedWaitHandleHelpers.GlobalPrefix 
            + new string('a', DistributedWaitHandleHelpers.MaxNameLength - DistributedWaitHandleHelpers.GlobalPrefix.Length);
        this.TestWorkingName(maxLengthName, NameStyle.Exact);
        this.TestBadName(maxLengthName + "a", NameStyle.Exact);
    }

    [Test]
    public void TestGarbageCollection()
    {
        var @lock = CreateLock("gc_test", NameStyle.AddPrefix);
        WeakReference AbandonLock() => new(@lock.Acquire());

        var weakHandle = AbandonLock();
        GC.Collect();
        GC.WaitForPendingFinalizers();

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

        new EventWaitHandleDistributedLock("").Name.ShouldEqual(@"Global\EMPTYz4PhNX7vuL3xVChQ1m2AB9Yg5AULVxXcg/SpIdNs6c5H0NE8XYXysP+DGNKHfuwvY7kxvUdBeoGlODJ6+SfaPg==");
        new EventWaitHandleDistributedLock("abc").Name.ShouldEqual(@"Global\abc");
        new EventWaitHandleDistributedLock("\\").Name.ShouldEqual(@"Global\_CgzRFsLFf7El/ZraEx9sqWRYeplYohSBSmI9sYIe1c4y2u7ECFoU4x2QCjV7HiVJMZsuDMLIz7r8akpKr+viAw==");
        new EventWaitHandleDistributedLock(new string('a', MaxNameLengthWithoutGlobalPrefix)).Name
            .ShouldEqual(@"Global\" + new string('a', MaxNameLengthWithoutGlobalPrefix));
        new EventWaitHandleDistributedLock(new string('\\', MaxNameLengthWithoutGlobalPrefix)).Name
            .ShouldEqual(@"Global\_____________________________________________________________________________________________________________________________________________________________________Y7DJXlpJeJjeX5XAOWV+ka/3ONBj5dHhKWcSH4pd5AC9YHFm+l1gBArGpBSBn3WcX00ArcDtKw7g24kJaHLifQ==");
        new EventWaitHandleDistributedLock(new string('x', MaxNameLengthWithoutGlobalPrefix + 1)).Name
            .ShouldEqual(@"Global\xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxsrCnXZ1XHiT//dOSBfAU0iC4Gtnlr0dQACBUK8Ev2OdRYJ9jcvbiqVCv/rjyPemTW9AvOonkdr0B2bG04gmeYA==");
    }

    private static EventWaitHandleDistributedLock CreateLock(string name, NameStyle nameStyle) => 
        new(
            (nameStyle == NameStyle.AddPrefix ? DistributedWaitHandleHelpers.GlobalPrefix + name : name), 
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
