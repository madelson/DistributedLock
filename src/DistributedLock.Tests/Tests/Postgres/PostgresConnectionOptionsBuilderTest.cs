using Medallion.Threading.Postgres;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Postgres;

[Category("CI")]
public class PostgresConnectionOptionsBuilderTest
{
    [Test]
    public void TestValidatesArguments()
    {
        var builder = new PostgresConnectionOptionsBuilder();
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.KeepaliveCadence(TimeSpan.FromMilliseconds(-2)));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.KeepaliveCadence(TimeSpan.MaxValue));

        Assert.Throws<ArgumentException>(() => PostgresConnectionOptionsBuilder.GetOptions(o => o.UseMultiplexing().UseTransaction()));
    }

    [Test]
    public void TestDefaults()
    {
        var options = PostgresConnectionOptionsBuilder.GetOptions(null);
        Assert.That(options.keepaliveCadence.IsInfinite, Is.True);
        Assert.That(options.useMultiplexing, Is.True);
        Assert.That(options.useTransaction, Is.False);
        options.ShouldEqual(PostgresConnectionOptionsBuilder.GetOptions(o => { }));
    }

    [Test]
    public void TestUseTransactionDoesNotRequireDisablingMultiplexing()
    {
        var options = PostgresConnectionOptionsBuilder.GetOptions(o => o.UseTransaction());
        Assert.That(options.useTransaction, Is.True);
        Assert.That(options.useMultiplexing, Is.False);
    }
}
