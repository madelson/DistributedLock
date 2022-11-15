using Medallion.Threading.SqlServer;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests.SqlServer;

[Category("CI")]
public class SqlConnectionOptionsBuilderTest
{
    [Test]
    public void TestValidatesArguments()
    {
        var builder = new SqlConnectionOptionsBuilder();
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.KeepaliveCadence(TimeSpan.FromMilliseconds(-2)));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.KeepaliveCadence(TimeSpan.MaxValue));

        Assert.Throws<ArgumentException>(() => SqlConnectionOptionsBuilder.GetOptions(o => o.UseMultiplexing().UseTransaction()));
    }

    [Test]
    public void TestDefaults()
    {
        var options = SqlConnectionOptionsBuilder.GetOptions(null);
        options.keepaliveCadence.ShouldEqual(TimeSpan.FromMinutes(10));
        Assert.IsTrue(options.useMultiplexing);
        Assert.IsFalse(options.useTransaction);
        options.ShouldEqual(SqlConnectionOptionsBuilder.GetOptions(o => { }));
    }
}
