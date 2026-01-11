using MongoDB.Driver;
using NUnit.Framework;
using MongoDB.Bson;
using Medallion.Shell;

namespace Medallion.Threading.Tests.MongoDB;

[SetUpFixture]
internal class MongoDBSetUpFixture
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var settings = MongoClientSettings.FromConnectionString(MongoDBCredentials.GetConnectionString(Environment.CurrentDirectory));
        if (IsMongoReady(settings, TimeSpan.FromSeconds(3))) { return; }

        // start mongo via docker
        const string ContainerName = "distributed-lock-mongo";
        DockerCommand(["stop", ContainerName]);
        DockerCommand(["rm", ContainerName]);
        var port = settings.Server.Port;
        DockerCommand(["run", "-d", "-p", $"{port}:{port}", "--name", ContainerName, "mongo:latest"]);

        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(15);
        for (var i = 0; i < 4; ++i)
        {
            if (IsMongoReady(settings, TimeSpan.FromSeconds(15))) { return; }
        }

        throw new Exception("Failed to start Mongo!");

        static bool DockerCommand(string[] args, bool throwOnError = false) =>
            Command.Run("docker", args, o => o.ThrowOnError(throwOnError))
                .RedirectTo(Console.Out)
                .RedirectStandardErrorTo(Console.Error)
                .Result.Success;
    }

    private static bool IsMongoReady(MongoClientSettings settings, TimeSpan timeout)
    {
        settings.ServerSelectionTimeout = settings.ConnectTimeout = settings.SocketTimeout = timeout;
        try
        {
            var client = new MongoClient(settings);
            var adminDb = client.GetDatabase("admin");

            adminDb.RunCommand<BsonDocument>(new BsonDocument("ping", 1));
            return true;
        }
        catch
        {
            return false;
        }
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() { }
}
