using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace Medallion.Threading.Tests.Postgres;

[SetUpFixture]
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
    public static async Task OneTimeTearDown() => await PostgreSql.DisposeAsync();
}