using NUnit.Framework;

namespace Medallion.Threading.Tests.Redis;

[SetUpFixture]
public class RedisSetUpFixture
{
    [OneTimeTearDown]
    public Task OneTimeTearDown() => RedisServer.DisposeAsync();
}
