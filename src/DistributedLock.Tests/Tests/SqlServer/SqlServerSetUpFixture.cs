using NUnit.Framework;
using Testcontainers.MsSql;

namespace Medallion.Threading.Tests.SqlServer;

[SetUpFixture]
public class SqlServerSetUpFixture
{
    public static MsSqlContainer SqlServer;

    [OneTimeSetUp]
    public static async Task OneTimeSetUp()
    {
        SqlServer = new MsSqlBuilder().Build();
        await SqlServer.StartAsync();
    }

    [OneTimeTearDown]
    public static async Task OneTimeTearDown() => await SqlServer.DisposeAsync();
}