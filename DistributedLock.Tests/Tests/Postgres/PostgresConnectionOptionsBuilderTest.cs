using Medallion.Threading.Postgres;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests.Postgres
{
    public class PostgresConnectionOptionsBuilderTest
    {
        [Test]
        public void TestValidatesArguments()
        {
            var builder = new PostgresConnectionOptionsBuilder();
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.KeepaliveCadence(TimeSpan.FromMilliseconds(-2)));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.KeepaliveCadence(TimeSpan.MaxValue));
        }

        [Test]
        public void TestDefaults()
        {
            var options = PostgresConnectionOptionsBuilder.GetOptions(null);
            Assert.IsTrue(options.keepaliveCadence.IsInfinite);
            Assert.IsTrue(options.useMultiplexing);
            options.ShouldEqual(PostgresConnectionOptionsBuilder.GetOptions(o => { }));
        }
    }
}
