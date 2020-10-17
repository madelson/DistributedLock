using Medallion.Threading.Internal;
using Medallion.Threading.Redis;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Redis
{
    public class RedisDistributedLockTest
    {
        private static string LockName => TestContext.CurrentContext.Test.FullName + TargetFramework.Current;

        [Test]
        public void TestName()
        {
            var name = "\0🐉汉字\b\r\n\\";
            var @lock = new RedisDistributedLock(name, CreateDatabaseMock().Object);
            @lock.As<IDistributedLock>().Name.ShouldEqual(name);
            @lock.Key.ShouldEqual(new RedisKey(name));
        }

        [Test]
        public void TestValidatesConstructorParameters()
        {
            var database = CreateDatabaseMock().Object;
            Assert.Throws<ArgumentNullException>(() => new RedisDistributedLock(default(RedisKey), database));
            Assert.Throws<ArgumentNullException>(() => new RedisDistributedLock(default(RedisKey), new[] { database }));
            Assert.Throws<ArgumentNullException>(() => new RedisDistributedLock("key", default(IDatabase)!));
            Assert.Throws<ArgumentNullException>(() => new RedisDistributedLock("key", default(IEnumerable<IDatabase>)!));
            Assert.Throws<ArgumentNullException>(() => new RedisDistributedLock("key", new[] { database, null! }));
            Assert.Throws<ArgumentException>(() => new RedisDistributedLock("key", Enumerable.Empty<IDatabase>()));
        }

        [Test]
        public void TestMajorityFaultingDatabasesCauseAcquireToThrow()
        {
            var databases = Enumerable.Range(0, 3).Select(_ => CreateDatabaseMock()).ToArray();
            MockDatabase(databases[0], () => throw new TimeZoneNotFoundException());
            MockDatabase(databases[2], () => throw new ArrayTypeMismatchException());

            var @lock = new RedisDistributedLock(LockName, databases.Select(d => d.Object));

            // we only get the one exception
            Assert.ThrowsAsync<TimeZoneNotFoundException>(() => @lock.TryAcquireAsync().AsTask());

            // single sync acquire flow is different
            var singleDatabaseLock = new RedisDistributedLock(LockName, databases[2].Object);
            Assert.Throws<ArrayTypeMismatchException>(() => singleDatabaseLock.Acquire());
        }

        [Test]
        [NonParallelizable] // timing-sensitive
        public async Task TestMajorityHangingDatabasesCauseAcquireToFail()
        {
            using var @event = new ManualResetEventSlim(initialState: false);
            var databases = Enumerable.Range(0, 3).Select(_ => CreateDatabaseMock()).ToArray();
            MockDatabase(databases[1], () => { @event.Wait(); return true; });
            MockDatabase(databases[2], () => { @event.Wait(); return false; });

            // use a high min validity time so that TryAcquireAsync() can return very quickly despite the hang
            var @lock = new RedisDistributedLock(
                LockName, 
                databases.Select(d => d.Object), 
                options: o => o.MinValidityTime(RedisDistributedLockOptionsBuilder.DefaultExpiry.TimeSpan - TimeSpan.FromSeconds(.2))
            );

            Assert.IsNull(await @lock.TryAcquireAsync());

            @event.Set(); // just to free the waiting threads
        }

        [Test]
        public void TestMajorityFaultingDatabasesCauseReleaseToThrow()
        {
            var databases = Enumerable.Range(0, 5).Select(_ => CreateDatabaseMock()).ToArray();

            var @lock = new RedisDistributedLock(LockName, databases.Select(d => d.Object));
            using var handle = @lock.Acquire();

            new List<int> { 1, 2, 4 }.ForEach(i => MockDatabase(databases[i], () => throw new DataMisalignedException()));
            var aggregateException = Assert.Throws<AggregateException>(() => handle.Dispose());
            Assert.IsInstanceOf<DataMisalignedException>(aggregateException.InnerException);
        }

        [Test]
        [NonParallelizable] // timing-sensitive
        public async Task TestAcquireFailsIfItTakesTooLong([Values] bool synchronous)
        {
            var database = CreateDatabaseMock();
            MockDatabase(database, () => { Thread.Sleep(50); return true; });

            var @lock = new RedisDistributedLock(
                LockName,
                database.Object,
                options: o => o.MinValidityTime(RedisDistributedLockOptionsBuilder.DefaultExpiry.TimeSpan - TimeSpan.FromMilliseconds(10))
            );

            // single sync acquire has different timeout logic, so we test it separately
            Assert.IsNull(synchronous ? @lock.TryAcquire() : await @lock.TryAcquireAsync());
        }

        [Test]
        [NonParallelizable] // timing-sensitive
        public async Task TestFailedAcquireReleasesWhatHasAlreadyBeenAcquired()
        {
            using var @event = new ManualResetEventSlim();
            var failDatabase = CreateDatabaseMock();
            MockDatabase(failDatabase, () => { @event.Wait(); return false; });

            var @lock = new RedisDistributedLock(LockName, new[] { RedisServer.GetDefaultServer(0).Multiplexer.GetDatabase(), failDatabase.Object });
            var acquireTask = @lock.TryAcquireAsync().AsTask();
            Assert.IsFalse(acquireTask.Wait(TimeSpan.FromMilliseconds(50)));
            @event.Set();
            Assert.IsNull(await acquireTask);

            var singleDatabaseLock = new RedisDistributedLock(LockName, RedisServer.GetDefaultServer(0).Multiplexer.GetDatabase());
            using var handle = await singleDatabaseLock.TryAcquireAsync();
            Assert.IsNotNull(handle);
        }

        private static Mock<IDatabase> CreateDatabaseMock()
        {
            var mock = new Mock<IDatabase>(MockBehavior.Strict);
            MockDatabase(mock, () => true);
            return mock;
        }

        private static void MockDatabase(Mock<IDatabase> mockDatabase, Func<bool> returns)
        {
            mockDatabase.Setup(d => d.StringSet(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                .Returns(returns);
            mockDatabase.Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                .Returns(() => Task.Run(returns));
            mockDatabase.Setup(d => d.ScriptEvaluate(It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
                .Returns(() => RedisResult.Create(returns()));
            mockDatabase.Setup(d => d.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
                .Returns(() => Task.Run(() => RedisResult.Create(returns())));
        }
    }
}
