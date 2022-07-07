using Medallion.Threading.MySql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests.MySql
{
    [Category("CI")]
    public class MySqlConnectionOptionsBuilderTest
    {
        [Test]
        public void TestValidatesArguments()
        {
            var builder = new MySqlConnectionOptionsBuilder();
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.KeepaliveCadence(TimeSpan.FromMilliseconds(-2)));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.KeepaliveCadence(TimeSpan.MaxValue));
        }

        [Test]
        public void TestDefaults()
        {
            var options = MySqlConnectionOptionsBuilder.GetOptions(null);
            options.keepaliveCadence.ShouldEqual(TimeSpan.FromHours(3.5));
            Assert.IsTrue(options.useMultiplexing);
            options.ShouldEqual(MySqlConnectionOptionsBuilder.GetOptions(o => { }));
        }
    }
}
