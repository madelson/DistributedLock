using Medallion.Threading.Oracle;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Medallion.Threading.Tests.Oracle
{
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
            Assert.IsTrue(options.useMultiplexing);
            options.ShouldEqual(OracleConnectionOptionsBuilder.GetOptions(o => { }));
        }
    }
}
