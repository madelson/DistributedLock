using MongoDB.Driver;
using System.IO;

namespace Medallion.Threading.Tests.MongoDB;

internal static class MongoDbCredentials
{
    private static string? _connectionString;

    public static string GetConnectionString(string baseDirectory)
    {
        if (_connectionString != null) { return _connectionString; }
        var file = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "credentials", "mongodb.txt"));
        if (File.Exists(file))
        {
            _connectionString = File.ReadAllText(file).Trim();
        }
        else
        {
            // Default local MongoDB connection
            _connectionString = "mongodb://localhost:27017";
        }
        return _connectionString;
    }

    public static IMongoDatabase GetDefaultDatabase(string baseDirectory)
    {
        var client = new MongoClient(GetConnectionString(baseDirectory));
        return client.GetDatabase("DistributedLockTests");
    }
}