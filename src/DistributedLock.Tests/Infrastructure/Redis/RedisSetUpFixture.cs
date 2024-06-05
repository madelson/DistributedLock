using NUnit.Framework;
using Testcontainers.Redis;

namespace Medallion.Threading.Tests.Redis;

[SetUpFixture]
public class RedisSetUpFixture
{
    public static RedisContainer Redis;

    [OneTimeSetUp]
    public static async Task OneTimeSetUp()
    {
        Redis = new RedisBuilder().Build();
        await Redis.StartAsync();
    }

    [OneTimeTearDown]
    public static async Task OneTimeTearDown() => await Redis.DisposeAsync();
}
