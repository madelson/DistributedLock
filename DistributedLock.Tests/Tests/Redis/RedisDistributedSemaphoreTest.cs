using Medallion.Threading.Redis;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Redis
{
    public class RedisDistributedSemaphoreTest
    {
        [Test]
        [Category("CI")]
        public void TestName()
        {
            const string Name = "\0🐉汉字\b\r\n\\";
            var @lock = new RedisDistributedSemaphore(Name, 1, new Mock<IDatabase>(MockBehavior.Strict).Object);
            @lock.Name.ShouldEqual(Name);
        }

        [Test]
        [Category("CI")]
        public void TestValidatesConstructorParameters()
        {
            var database = new Mock<IDatabase>(MockBehavior.Strict).Object;
            Assert.Throws<ArgumentNullException>(() => new RedisDistributedSemaphore(default!, 2, database));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RedisDistributedSemaphore("key", 0, database));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RedisDistributedSemaphore("key", -1, database));
            Assert.Throws<ArgumentNullException>(() => new RedisDistributedSemaphore("key", 2, default(IDatabase)!));
        }
    }
}
