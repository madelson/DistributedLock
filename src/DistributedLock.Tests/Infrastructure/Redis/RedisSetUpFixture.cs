using NUnit.Framework;

namespace Medallion.Threading.Tests.Redis;

[SetUpFixture]
public class RedisSetUpFixture
{
    [OneTimeSetUp]
    public void OneTimeSetUp() { }

    [OneTimeTearDown]
    public void OneTimeTearDown() => RedisServer.DisposeAll();
}
