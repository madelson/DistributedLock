using Medallion.Threading.MongoDB;
using MongoDB.Driver;

namespace Medallion.Threading.Tests.MongoDB;

public sealed class TestingMongoDistributedLockProvider : TestingLockProvider<TestingMongoDbSynchronizationStrategy>
{
    private const string CollectionName = "distributed.locks";
    private readonly IMongoDatabase _database = MongoDbCredentials.GetDefaultDatabase(Environment.CurrentDirectory);

    public override IDistributedLock CreateLockWithExactName(string name)
    {
        // Use a short expiry to make tests like TestHandleLostTriggersCorrectly run faster
        var @lock = new MongoDistributedLock(name, _database, CollectionName, options => options.Expiry(TimeSpan.FromSeconds(5)));
        this.Strategy.KillHandleAction = () =>
        {
            var collection = _database.GetCollection<MongoLockDocument>(CollectionName);
            collection.DeleteOne(Builders<MongoLockDocument>.Filter.Eq(d => d.Id, name));
        };
        return @lock;
    }

    public override string GetSafeName(string name)
    {
        return new MongoDistributedLock(name, _database, CollectionName).Name;
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
            _database.DropCollection(CollectionName);
        }
        catch
        {
            // Ignore cleanup errors
        }
        base.Dispose();
    }
}