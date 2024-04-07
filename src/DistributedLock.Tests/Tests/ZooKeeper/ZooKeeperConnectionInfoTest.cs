using Medallion.Threading.ZooKeeper;
using NUnit.Framework;

namespace Medallion.Threading.Tests.ZooKeeper;

[Category("CI")]
public class ZooKeeperConnectionInfoTest
{
    [Test]
    public void TestEquality()
    {
        var connectionA = new ZooKeeperConnectionInfo(
            "cs",
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            new EquatableReadOnlyList<ZooKeeperAuthInfo>(new[] { new ZooKeeperAuthInfo("s", new EquatableReadOnlyList<byte>(new byte[] { 10 })) })
        );
        var connectionB = new ZooKeeperConnectionInfo(
            "cs",
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            new EquatableReadOnlyList<ZooKeeperAuthInfo>(new[] { new ZooKeeperAuthInfo("s", new EquatableReadOnlyList<byte>(new byte[] { 10 })) })
        );
        var connectionC = connectionA with { 
            AuthInfo = new EquatableReadOnlyList<ZooKeeperAuthInfo>(new[]
            {
                new ZooKeeperAuthInfo("s", new EquatableReadOnlyList<byte>(new byte[] { 10 })),
                new ZooKeeperAuthInfo("s2", new EquatableReadOnlyList<byte>(new byte[] { 11 })),
            })
        };

        Assert.That(connectionA == connectionB, Is.True);
        connectionA.GetHashCode().ShouldEqual(connectionB.GetHashCode());
        Assert.That(connectionA == connectionC, Is.False);
        Assert.That(connectionC.GetHashCode(), Is.Not.EqualTo(connectionA.GetHashCode()));
    }
}
