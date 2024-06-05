using StackExchange.Redis;
using Testcontainers.Redis;

namespace Medallion.Threading.Tests.Redis;

internal class RedisServer
{
    public RedisServer(int port, bool allowAdmin)
    {
        this.Port = port;
        this.Multiplexer = ConnectionMultiplexer.Connect($"localhost:{this.Port},abortConnect=false{(allowAdmin ? ",allowAdmin=true" : string.Empty)}");
        // Clean the db to ensure it is empty. Running an arbitrary command also ensures that 
        // the db successfully spun up before we proceed (Connect seemingly can complete before that happens). 
        // This is particularly important for cross-process locking where the lock taker process
        // assumes we've already started a server on certain ports.
        this.Multiplexer.GetDatabase().Execute("flushall", Array.Empty<object>(), CommandFlags.DemandMaster);
    }

    public int Port { get; }
    public ConnectionMultiplexer Multiplexer { get; }
    
    public static RedisServer Create(RedisContainer container, bool allowAdmin = false) 
        => new RedisServer(container.GetMappedPublicPort(RedisBuilder.RedisPort), allowAdmin);
    public static IDatabase CreateDatabase(RedisContainer container, bool allowAdmin = false) 
        => Create(container, allowAdmin).Multiplexer.GetDatabase();
}
