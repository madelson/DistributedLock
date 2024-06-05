using Medallion.Threading.Redis;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;
using StackExchange.Redis.KeyspaceIsolation;

namespace Medallion.Threading.Tests.Redis;

public abstract class RedisSynchronizationCoreTestCases<TLockProvider>
    // note: we arbitrarily use the single db provider because we will be overriding the set of dbs and so we don't
    // want to see cases for each possible db provider type
    where TLockProvider : TestingLockProvider<TestingRedisSynchronizationStrategy<TestingRedis3DatabaseProvider>>, new()
{
    private TLockProvider _provider = default!;

    [SetUp]
    public async Task SetUp()
    {
        this._provider = new TLockProvider();
        await this._provider.SetupAsync();
    }
    [TearDown]
    public async Task TearDown() => await this._provider.DisposeAsync();

    [Test]
    [Retry(tryCount: 3)] // unstable in CI
    public void TestMajorityFaultingDatabasesCauseAcquireToThrow()
    {
        var databases = Enumerable.Range(0, 3).Select(_ => CreateDatabaseMock()).ToArray();
        MockDatabase(databases[0], () => throw new TimeZoneNotFoundException());
        MockDatabase(databases[2], () => throw new ArrayTypeMismatchException());

        this._provider.Strategy.DatabaseProvider.Databases = databases.Select(d => d.Object).ToArray();
        var @lock = this._provider.CreateLock("multi");

        // we only get the one exception
        Assert.ThrowsAsync<TimeZoneNotFoundException>(() => @lock.TryAcquireAsync().AsTask());

        // single sync acquire flow is different
        this._provider.Strategy.DatabaseProvider.Databases = new[] { databases[2].Object };
        var singleDatabaseLock = this._provider.CreateLock("single");
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

        this._provider.Strategy.DatabaseProvider.Databases = databases.Select(d => d.Object).ToArray();
        // use a high min validity time so that TryAcquireAsync() can return very quickly despite the hang
        this._provider.Strategy.SetOptions(o => o.MinValidityTime(RedisDistributedSynchronizationOptionsBuilder.DefaultExpiry.TimeSpan - TimeSpan.FromSeconds(.2)));
        var @lock = this._provider.CreateLock("lock");

        Assert.That(await @lock.TryAcquireAsync(), Is.Null);

        @event.Set(); // just to free the waiting threads
    }

    [Test]
    public void TestMajorityFaultingDatabasesCauseReleaseToThrow()
    {
        var databases = Enumerable.Range(0, 5).Select(_ => CreateDatabaseMock()).ToArray();
        this._provider.Strategy.DatabaseProvider.Databases = databases.Select(d => d.Object).ToArray();
        var @lock = this._provider.CreateLock("lock");
        using var handle = @lock.Acquire();

        new List<int> { 1, 2, 4 }.ForEach(i => MockDatabase(databases[i], () => throw new DataMisalignedException()));
        var aggregateException = Assert.Throws<AggregateException>(() => handle.Dispose())!;
        Assert.That(aggregateException.InnerExceptions, Has.Count.EqualTo(3));
        Assert.That(aggregateException.InnerExceptions, Is.All.InstanceOf<DataMisalignedException>());
    }

    [Test]
    public void TestHalfFaultingDatabasesCauseAcquireToThrow()
    {
        var databases = Enumerable.Range(0, 2).Select(_ => CreateDatabaseMock()).ToArray();
        MockDatabase(databases[0], () => throw new TimeZoneNotFoundException());
        this._provider.Strategy.DatabaseProvider.Databases = databases.Select(d => d.Object).ToArray();

        var @lock = this._provider.CreateLock("lock");
        Assert.Throws<TimeZoneNotFoundException>(() => @lock.Acquire(TimeSpan.FromSeconds(10)));
    }

    [Test]
    [NonParallelizable, Retry(tryCount: 3)] // timing-sensitive
    public async Task TestAcquireFailsIfItTakesTooLong([Values] bool synchronous)
    {
        var database = CreateDatabaseMock();
        MockDatabase(database, () => { Thread.Sleep(50); return true; });

        this._provider.Strategy.DatabaseProvider.Databases = new[] { database.Object };
        this._provider.Strategy.SetOptions(o => o.MinValidityTime(RedisDistributedSynchronizationOptionsBuilder.DefaultExpiry.TimeSpan - TimeSpan.FromMilliseconds(10)));
        var @lock = this._provider.CreateLock("lock");

        // single sync acquire has different timeout logic, so we test it separately
        Assert.That(synchronous ? @lock.TryAcquire() : await @lock.TryAcquireAsync(), Is.Null);
    }

    [Test]
    [NonParallelizable] // timing-sensitive
    public async Task TestFailedAcquireReleasesWhatHasAlreadyBeenAcquired()
    {
        using var @event = new ManualResetEventSlim();
        var failDatabase = CreateDatabaseMock();
        MockDatabase(failDatabase, () => { @event.Wait(); return false; });

        this._provider.Strategy.DatabaseProvider.Databases = new[] { RedisServer.CreateDatabase(_provider.Strategy.DatabaseProvider.Redis), failDatabase.Object };
        var @lock = this._provider.CreateLock("lock");

        var acquireTask = @lock.TryAcquireAsync().AsTask();
        Assert.That(acquireTask.Wait(TimeSpan.FromMilliseconds(50)), Is.False);
        @event.Set();
        Assert.That(await acquireTask, Is.Null);

        this._provider.Strategy.DatabaseProvider.Databases = new[] { RedisServer.CreateDatabase(_provider.Strategy.DatabaseProvider.Redis) };
        var singleDatabaseLock = this._provider.CreateLock("lock");
        using var handle = await singleDatabaseLock.TryAcquireAsync();
        Assert.That(handle, Is.Not.Null);
    }

    [Test]
    public void TestAcquireWithLockPrefix()
    {
        this._provider.Strategy.DatabaseProvider.Databases = new[] { CreateDatabase(keyPrefix: "P") };
        var implicitPrefixLock = this._provider.CreateLock("N");

        this._provider.Strategy.DatabaseProvider.Databases = new[] { CreateDatabase() };
        var noPrefixLock = this._provider.CreateLock("N");
        var explicitPrefixLock = this._provider.CreateLock("PN");

        using var implicitPrefixHandle = implicitPrefixLock.TryAcquire();
        Assert.That(implicitPrefixHandle, Is.Not.Null);
        using var noPrefixHandle = noPrefixLock.TryAcquire();
        Assert.That(noPrefixHandle, Is.Not.Null);
        using var explicitPrefixHandle = explicitPrefixLock.TryAcquire();
        Assert.That(explicitPrefixHandle, Is.Null);

        IDatabase CreateDatabase(string? keyPrefix = null)
        {
            var database = RedisServer.CreateDatabase(_provider.Strategy.DatabaseProvider.Redis);
            return keyPrefix is null ? database : database.WithKeyPrefix(keyPrefix);
        }
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
        mockDatabase.Setup(d => d.ScriptEvaluate(It.IsAny<LuaScript>(), It.IsAny<object>(), It.IsAny<CommandFlags>()))
            .Returns(() => RedisResult.Create(returns()));
        mockDatabase.Setup(d => d.ScriptEvaluateAsync(It.IsAny<LuaScript>(), It.IsAny<object>(), It.IsAny<CommandFlags>()))
            .Returns(() => Task.Run(() => RedisResult.Create(returns())));
        mockDatabase.Setup(d => d.SortedSetRemove(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .Returns(() => (bool)RedisResult.Create(returns()));
        mockDatabase.Setup(d => d.SortedSetRemoveAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .Returns(() => Task.Run(() => (bool)RedisResult.Create(returns())));
        mockDatabase.Setup(d => d.IsConnected(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Returns(true);
    }
}
