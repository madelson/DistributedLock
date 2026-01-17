using Medallion.Threading.MongoDB;
using MongoDB.Driver;

namespace Medallion.Threading.Tests.MongoDB;

public sealed class TestingMongoDistributedLockProvider : TestingLockProvider<TestingMongoDbSynchronizationStrategy>
{
    private readonly IMongoDatabase _database = MongoDBCredentials.GetDefaultDatabase(Environment.CurrentDirectory);

    public override IDistributedLock CreateLockWithExactName(string name)
    {
        // Use a short expiry to make tests like TestHandleLostTriggersCorrectly run faster
        var @lock = new MongoDistributedLock(name, this._database, options => options.Expiry(TimeSpan.FromSeconds(5)).ExtensionCadence(TimeSpan.FromSeconds(.5)));
        this.Strategy.KillHandleAction = () =>
        {
            var collection = this._database.GetCollection<MongoLockDocument>(MongoDistributedLock.DefaultCollectionName);
            collection.DeleteOne(Builders<MongoLockDocument>.Filter.Eq(d => d.Id, name));
        };
        return @lock;
    }

    public override string GetSafeName(string name) =>
        new MongoDistributedLock(name, this._database, MongoDistributedLock.DefaultCollectionName).Name;

    public override string GetCrossProcessLockType() => nameof(MongoDistributedLock);

    public override void Dispose()
    {
        this._database.DropCollection(MongoDistributedLock.DefaultCollectionName);
        base.Dispose();
    }
}