using MongoDB.Driver;
using System.Collections.Concurrent;

namespace Medallion.Threading.Tests.MongoDB;

internal static class MongoDBCredentials
{
    private static readonly ConcurrentDictionary<string, string> ConnectionStringsByBaseDirectory = [];

    public static string GetConnectionString(string baseDirectory) =>
        ConnectionStringsByBaseDirectory.GetOrAdd(baseDirectory, static d =>
        {
            var file = Path.GetFullPath(Path.Combine(d, "..", "..", "..", "credentials", "mongodb.txt"));
            return File.Exists(file)
                ? File.ReadAllText(file).Trim()
                // Default local MongoDB connection
                : "mongodb://localhost:27017";
        });

    public static IMongoDatabase GetDefaultDatabase(string baseDirectory)
    {
        var client = new MongoClient(GetConnectionString(baseDirectory));
        return client.GetDatabase("distributedLockTests");
    }
}