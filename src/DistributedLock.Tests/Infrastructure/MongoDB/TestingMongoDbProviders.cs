using Medallion.Threading.MongoDB;
using MongoDB.Driver;

namespace Medallion.Threading.Tests.MongoDB;

public sealed class TestingMongoDistributedLockProvider : TestingLockProvider<TestingMongoDbSynchronizationStrategy>
{
    private readonly string _collectionName = "DistributedLocks_" + Guid.NewGuid().ToString("N");
    private readonly IMongoDatabase _database = MongoDbCredentials.GetDefaultDatabase(Environment.CurrentDirectory);

    public override IDistributedLock CreateLockWithExactName(string name)
    {
        var @lock = new MongoDistributedLock(name, _database, _collectionName);
        Strategy.KillHandleAction = () =>
        {
            var collection = _database.GetCollection<MongoLockDocument>(_collectionName);
            collection.DeleteOne(Builders<MongoLockDocument>.Filter.Eq(d => d.Id, name));
        };
        return @lock;
    }

    public override string GetSafeName(string name)
    {
        return new MongoDistributedLock(name, _database, _collectionName).Name;
    }

    public override string GetCrossProcessLockType()
    {
        return nameof(MongoDistributedLock);
    }

    public override void Dispose()
    {
        // Clean up test collection
        try
        {
            _database.DropCollection(_collectionName);
        }
        catch
        {
            // Ignore cleanup errors
        }
        base.Dispose();
    }
}