using StackExchange.Redis;
using StackExchange.Redis.KeyspaceIsolation;
using Testcontainers.Redis;

namespace Medallion.Threading.Tests.Redis;

public abstract class TestingRedisDatabaseProvider
{
    // publicly settable so that callers can alter the dbs in use
    public IReadOnlyList<IDatabase> Databases { get; set; } = default!;
    public RedisContainer Redis { get; protected set; } = default!;
    public abstract string ConnectionStrings { get; }

    public virtual string CrossProcessLockTypeSuffix => this.Databases.Count.ToString();

    public abstract ValueTask SetupAsync();
    public abstract ValueTask DisposeAsync();
}

public sealed class TestingRedisSingleDatabaseProvider : TestingRedisDatabaseProvider
{
    public override string ConnectionStrings => this.Redis.GetConnectionString();
    public override async ValueTask SetupAsync()
    {
        this.Redis = new RedisBuilder().Build();
        await this.Redis.StartAsync();

        this.Databases = [RedisServer.CreateDatabase(this.Redis)];
    }

    public override ValueTask DisposeAsync() => this.Redis.DisposeAsync();
}

public sealed class TestingRedisWithKeyPrefixSingleDatabaseProvider : TestingRedisDatabaseProvider
{
    public override string ConnectionStrings => this.Redis.GetConnectionString();
    public override string CrossProcessLockTypeSuffix => "1WithPrefix";

    public override async ValueTask SetupAsync()
    {
        this.Redis = new RedisBuilder().Build();
        await this.Redis.StartAsync();

        this.Databases = [RedisServer.CreateDatabase(this.Redis).WithKeyPrefix("distributed_locks:")];
    }

    public override ValueTask DisposeAsync() => this.Redis.DisposeAsync();
}

public sealed class TestingRedis3DatabaseProvider : TestingRedisDatabaseProvider
{
    private RedisContainer[] _redises = default!;

    public override string ConnectionStrings => string.Join("||", _redises.Select(x => x.GetConnectionString()));

    public override async ValueTask SetupAsync()
    {
        this._redises = Enumerable.Range(0, 3).Select(_ => new RedisBuilder().Build()).ToArray();
        await Task.WhenAll(this._redises.Select(redis => redis.StartAsync()));

        this.Redis = _redises[0];

        this.Databases = this._redises.Select(redis => RedisServer.CreateDatabase(redis)).ToArray();
    }

    public override async ValueTask DisposeAsync()
    {
        foreach (var redis in this._redises)
        {
            await redis.DisposeAsync();
        }
    }
}

public sealed class TestingRedis2x1DatabaseProvider : TestingRedisDatabaseProvider
{
    private static IDatabase DeadDatabase;
    private RedisContainer[] _redises = default!;

    public override string ConnectionStrings => string.Join("||", _redises.Select(x => x.GetConnectionString()));

    static TestingRedis2x1DatabaseProvider()
    {
        var redis = new RedisBuilder().Build();
        redis.StartAsync().GetAwaiter().GetResult();
        var server = RedisServer.Create(redis, allowAdmin: true);
        DeadDatabase = server.Multiplexer.GetDatabase();
        server.Multiplexer.GetServer($"localhost:{server.Port}").Shutdown(ShutdownMode.Never);
        redis.StopAsync().GetAwaiter().GetResult();
    }

    public override async ValueTask SetupAsync()
    {
        this._redises = Enumerable.Range(0, 2).Select(_ => new RedisBuilder().Build()).ToArray();
        await Task.WhenAll(this._redises.Select(redis => redis.StartAsync()));

        this.Redis = _redises[0];

        Databases = this._redises.Select(redis => RedisServer.CreateDatabase(redis)).Append(DeadDatabase).ToArray();
    }

    public override async ValueTask DisposeAsync()
    {
        foreach (var redis in this._redises)
        {
            await redis.DisposeAsync();
        }
    }

    public override string CrossProcessLockTypeSuffix => "2x1";
}
