using Medallion.Threading.Tests.Postgres;
using NUnit.Framework;

namespace Medallion.Threading.Tests;

public class GlobalSetupTest
{

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        // await PostgresDb.Container.StartAsync();
    }

    [OneTimeTearDown]
    public async Task GlobalTeardown()
    {
        await PostgresDb.Container.StopAsync();
    }
}