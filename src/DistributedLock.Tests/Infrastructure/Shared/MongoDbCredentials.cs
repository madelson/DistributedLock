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
        _connectionString = File.Exists(file)
                                ? File.ReadAllText(file).Trim()
                                // Default local MongoDB connection
                                : "mongodb://localhost:27017";
        return _connectionString;
    }

    public static IMongoDatabase GetDefaultDatabase(string baseDirectory)
    {
        var client = new MongoClient(GetConnectionString(baseDirectory));
        return client.GetDatabase("distributedLockTests");
    }
}