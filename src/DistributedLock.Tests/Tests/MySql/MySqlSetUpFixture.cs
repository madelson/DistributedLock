using NUnit.Framework;
using Testcontainers.MariaDb;
using Testcontainers.MySql;

namespace Medallion.Threading.Tests.MySql;

[SetUpFixture]
public class MySqlSetUpFixture
{
    public static MySqlContainer MySql;
    public static MariaDbContainer MariaDb;

    [OneTimeSetUp]
    public static async Task OneTimeSetUp()
    {
        MySql = new MySqlBuilder().Build();
        MariaDb = new MariaDbBuilder().Build();

        await Task.WhenAll(MySql.StartAsync(), MariaDb.StartAsync());
    }

    [OneTimeTearDown]
    public static async Task OneTimeTearDown()
    {
        await MySql.DisposeAsync();
        await MariaDb.DisposeAsync();
    }
}