using Testcontainers.PostgreSql;

namespace Medallion.Threading.Tests.Postgres;

public class PostgresDb
{
    public static PostgreSqlContainer Container { get; } = new PostgreSqlBuilder().Build();

    static PostgresDb()
    {
        Container.StartAsync().Wait();
    }
}