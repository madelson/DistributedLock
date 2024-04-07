using Medallion.Threading.Oracle;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Oracle;

[Category("CI")]
public class OracleConnectionOptionsBuilderTest
{
    [Test]
    public void TestValidatesArguments()
    {
        var builder = new OracleConnectionOptionsBuilder();
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.KeepaliveCadence(TimeSpan.FromMilliseconds(-2)));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.KeepaliveCadence(TimeSpan.MaxValue));
    }

    [Test]
    public void TestDefaults()
    {
        var options = OracleConnectionOptionsBuilder.GetOptions(null);
        options.keepaliveCadence.ShouldEqual(Timeout.InfiniteTimeSpan);
        Assert.That(options.useMultiplexing, Is.True);
        options.ShouldEqual(OracleConnectionOptionsBuilder.GetOptions(o => { }));
    }
}
