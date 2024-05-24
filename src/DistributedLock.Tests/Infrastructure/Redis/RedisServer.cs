using StackExchange.Redis;
using Testcontainers.Redis;

namespace Medallion.Threading.Tests.Redis;

internal class RedisServer
{
    // redis default is 6379, so go one above that
    private static readonly int MinDynamicPort = RedisPorts.DefaultPorts.Max() + 1, MaxDynamicPort = MinDynamicPort + 100;

    public static async Task DisposeAsync()
    {
        foreach (var container in RedisContainers)
        {
            await container.StopAsync();
        }
    }

    private static readonly List<RedisContainer> RedisContainers = [];
    private static readonly RedisServer[] DefaultServers = new RedisServer[RedisPorts.DefaultPorts.Count];

    private readonly RedisContainer _redis;

    public RedisServer(bool allowAdmin = false)
    {
        _redis = new RedisBuilder()
            .WithPortBinding(MinDynamicPort + RedisContainers.Count)
            .Build();
        _redis.StartAsync().Wait();
        RedisContainers.Add(_redis);

        this.Port = _redis.GetMappedPublicPort(RedisBuilder.RedisPort);

        this.Multiplexer = ConnectionMultiplexer.Connect($"localhost:{this.Port},abortConnect=false{(allowAdmin ? ",allowAdmin=true" : string.Empty)}");
        // Clean the db to ensure it is empty. Running an arbitrary command also ensures that 
        // the db successfully spun up before we proceed (Connect seemingly can complete before that happens). 
        // This is particularly important for cross-process locking where the lock taker process
        // assumes we've already started a server on certain ports.
        this.Multiplexer.GetDatabase().Execute("flushall", Array.Empty<object>(), CommandFlags.DemandMaster);
    }

    public int Port { get; }
    public ConnectionMultiplexer Multiplexer { get; }

    public void Dispose() => _redis.DisposeAsync().GetAwaiter().GetResult();

    public static RedisServer GetDefaultServer(int index)
    {
        lock (DefaultServers)
        {
            return DefaultServers[index] ??= new RedisServer(allowAdmin: false);
        }
    }
}
