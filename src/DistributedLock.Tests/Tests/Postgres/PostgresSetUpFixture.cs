using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace Medallion.Threading.Tests.Postgres;

public class PostgresSetUpFixture
{
    public static PostgreSqlContainer PostgreSql;

    [OneTimeSetUp]
    public static async Task OneTimeSetUp()
    {
        PostgreSql = new PostgreSqlBuilder().Build();
        await PostgreSql.StartAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown() => await PostgreSql.DisposeAsync();
}