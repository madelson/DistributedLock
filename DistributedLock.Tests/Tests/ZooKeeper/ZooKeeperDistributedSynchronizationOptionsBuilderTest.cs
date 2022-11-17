using Medallion.Threading.ZooKeeper;
using NUnit.Framework;

namespace Medallion.Threading.Tests.ZooKeeper;

[Category("CI")]
public class ZooKeeperDistributedSynchronizationOptionsBuilderTest
{
    [Test]
    public void TestValidatesArguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(Create(b => b.ConnectTimeout(TimeSpan.FromSeconds(-2))));
        Assert.Throws<ArgumentOutOfRangeException>(Create(b => b.SessionTimeout(TimeSpan.FromSeconds(-2))));
        Assert.Throws<ArgumentOutOfRangeException>(Create(b => b.SessionTimeout(TimeSpan.Zero)));
        Assert.Throws<ArgumentOutOfRangeException>(Create(b => b.SessionTimeout(Timeout.InfiniteTimeSpan)));
        Assert.Throws<ArgumentNullException>(Create(b => b.AddAuthInfo(null!, Array.Empty<byte>())));
        Assert.Throws<ArgumentNullException>(Create(b => b.AddAuthInfo("scheme", null!)));
        Assert.Throws<ArgumentNullException>(Create(b => b.AddAccessControl(null!, "id", 0x1f)));
        Assert.Throws<ArgumentNullException>(Create(b => b.AddAccessControl("scheme", null!, 0x1f)));

        static TestDelegate Create(Action<ZooKeeperDistributedSynchronizationOptionsBuilder> action) =>
            () => ZooKeeperDistributedSynchronizationOptionsBuilder.GetOptions(action);
    }
}
