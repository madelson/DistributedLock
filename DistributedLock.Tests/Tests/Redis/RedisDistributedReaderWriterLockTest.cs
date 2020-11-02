using Medallion.Threading.Redis;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Medallion.Threading.Tests.Tests.Redis
{
    [Category("CI")]
    public class RedisDistributedReaderWriterLockTest
    {
        [Test]
        public void TestName()
        {
            const string Name = "\0🐉汉字\b\r\n\\";
            var @lock = new RedisDistributedReaderWriterLock(Name, new Mock<IDatabase>(MockBehavior.Strict).Object);
            @lock.Name.ShouldEqual(Name);
        }

        [Test]
        public void TestValidatesConstructorParameters()
        {
            var database = new Mock<IDatabase>(MockBehavior.Strict).Object;
            Assert.Throws<ArgumentNullException>(() => new RedisDistributedReaderWriterLock(default, database));
            Assert.Throws<ArgumentNullException>(() => new RedisDistributedReaderWriterLock(default, new[] { database }));
            Assert.Throws<ArgumentNullException>(() => new RedisDistributedReaderWriterLock("key", default(IDatabase)!));
            Assert.Throws<ArgumentNullException>(() => new RedisDistributedReaderWriterLock("key", default(IEnumerable<IDatabase>)!));
            Assert.Throws<ArgumentNullException>(() => new RedisDistributedReaderWriterLock("key", new[] { database, null! }));
            Assert.Throws<ArgumentException>(() => new RedisDistributedReaderWriterLock("key", Enumerable.Empty<IDatabase>()));
            Assert.Throws<ArgumentException>(() => new RedisDistributedReaderWriterLock("key", new[] { database }, o => o.Expiry(TimeSpan.FromSeconds(0.2))));
        }
    }
}
