using Medallion.Threading.MongoDB;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;

namespace Medallion.Threading.Tests.MongoDB;

public class MongoDistributedLockTest
{
    [Test]
    public async Task TestBasicLockFunctionality()
    {
        var database = MongoDbCredentials.GetDefaultDatabase(Environment.CurrentDirectory);
        var lockName = TestHelper.UniqueName;
        var @lock = new MongoDistributedLock(lockName, database);
        await using (var handle = await @lock.AcquireAsync())
        {
            Assert.That(handle, Is.Not.Null);
            // Use async TryAcquireAsync instead of synchronous IsHeld()
            var handle2 = await @lock.TryAcquireAsync(TimeSpan.Zero);
            Assert.That(handle2, Is.Null, "Lock should be held");
            if (handle2 != null)
            {
                await handle2.DisposeAsync();
            }
        }
        // Verify lock is released
        await using (var handle = await @lock.TryAcquireAsync(TimeSpan.FromSeconds(1)))
        {
            Assert.That(handle, Is.Not.Null, "Lock should be released");
        }
    }

    [Test]
    public async Task TestCustomCollectionName()
    {
        var database = MongoDbCredentials.GetDefaultDatabase(Environment.CurrentDirectory);
        var lockName = TestHelper.UniqueName;
        const string CustomCollectionName = "CustomLocks";
        var @lock = new MongoDistributedLock(lockName, database, CustomCollectionName);
        await using (var handle = await @lock.AcquireAsync())
        {
            Assert.That(handle, Is.Not.Null);
        }

        // Verify the collection was created
        var collectionExists = (await database.ListCollectionNamesAsync()).ToList().Contains(CustomCollectionName);
        Assert.That(collectionExists, Is.True);

        // Cleanup
        await database.DropCollectionAsync(CustomCollectionName);
    }

    [Test]
    public async Task TestHandleLostToken()
    {
        var database = MongoDbCredentials.GetDefaultDatabase(Environment.CurrentDirectory);
        var lockName = TestHelper.UniqueName;
        // Configure a short extension cadence so the test doesn't have to wait too long
        var @lock = new MongoDistributedLock(lockName, database, options: o => o.ExtensionCadence(TimeSpan.FromMilliseconds(500)));
        await using var handle = await @lock.AcquireAsync();
        Assert.That(handle, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(handle.HandleLostToken.CanBeCanceled, Is.True);
            Assert.That(handle.HandleLostToken.IsCancellationRequested, Is.False);
        });

        // Manually delete the lock document to simulate lock loss
        var collection = database.GetCollection<MongoLockDocument>("distributed.locks");
        await collection.DeleteOneAsync(Builders<MongoLockDocument>.Filter.Eq(d => d.Id, lockName));

        // Wait a bit for the extension task to detect the loss
        await Task.Delay(TimeSpan.FromSeconds(2));
        Assert.That(handle.HandleLostToken.IsCancellationRequested, Is.True, "HandleLostToken should be signaled when lock is lost");
    }

    [Test]
    public async Task TestLockContentionAsync()
    {
        var database = MongoDbCredentials.GetDefaultDatabase(Environment.CurrentDirectory);
        var lockName = TestHelper.UniqueName;
        var lock1 = new MongoDistributedLock(lockName, database);
        var lock2 = new MongoDistributedLock(lockName, database);
        await using (var handle1 = await lock1.AcquireAsync())
        {
            Assert.That(handle1, Is.Not.Null);
            var handle2 = await lock2.TryAcquireAsync(TimeSpan.FromMilliseconds(100));
            Assert.That(handle2, Is.Null, "Should not acquire lock while held by another instance");
        }

        // After release, lock2 should be able to acquire
        await using (var handle2 = await lock2.AcquireAsync(TimeSpan.FromSeconds(5)))
        {
            Assert.That(handle2, Is.Not.Null);
        }
    }

    [Test]
    [Category("CI")]
    public void TestName()
    {
        const string Name = "\0🐉汉字\b\r\n\\";
        var database = new Mock<IMongoDatabase>(MockBehavior.Strict).Object;
        var @lock = new MongoDistributedLock(Name, database);
        // With safe name conversion, the key should be the same as name since it's within byte limits
        @lock.Name.ShouldEqual(@lock.Key);
    }

    [Test]
    [Category("CI")]
    public void TestReturnsUnmodifiedKey()
    {
        const string Key = "my-exact-key";
        var database = new Mock<IMongoDatabase>(MockBehavior.Strict).Object;
        var @lock = new MongoDistributedLock(Key, database);
        @lock.Key.ShouldEqual(Key);
        @lock.Name.ShouldEqual(Key);
    }

    [Test]
    [Category("CI")]
    public void TestValidatesEmptyKey()
    {
        var database = new Mock<IMongoDatabase>(MockBehavior.Strict).Object;
        Assert.Throws<FormatException>(() => new MongoDistributedLock(string.Empty, database));
    }

    [Test]
    [Category("CI")]
    public void TestValidatesKeyTooLong()
    {
        var database = new Mock<IMongoDatabase>(MockBehavior.Strict).Object;
        // Create a string that's exactly 256 bytes in UTF-8 (over the 255 limit)
        var longKey = new string('a', 256);
        Assert.Throws<FormatException>(() => new MongoDistributedLock(longKey, database));
    }

    [Test]
    [Category("CI")]
    public void TestValidatesMultibyteCharacters()
    {
        var database = new Mock<IMongoDatabase>(MockBehavior.Strict).Object;
        // Chinese characters are 3 bytes each in UTF-8
        // 86 characters * 3 bytes = 258 bytes (over 255 limit)
        var multibyteLongKey = new string('汉', 86);
        Assert.Throws<FormatException>(() => new MongoDistributedLock(multibyteLongKey, database));
    }

    [Test]
    [Category("CI")]
    public void TestValidatesConstructorParameters()
    {
        var database = new Mock<IMongoDatabase>(MockBehavior.Strict).Object;
        Assert.Throws<ArgumentNullException>(() => new MongoDistributedLock(null!, database));
        Assert.Throws<ArgumentNullException>(() => new MongoDistributedLock("key", null!));
        Assert.Throws<ArgumentNullException>(() => new MongoDistributedLock("key", database, (string)null!));
    }

    [Test]
    public async Task TestIndexExistence()
    {
        var database = MongoDbCredentials.GetDefaultDatabase(Environment.CurrentDirectory);
        var collectionName = "TestIndex" + Guid.NewGuid().ToString("N");
        
        var @lock = new MongoDistributedLock("lock", database, collectionName);
        await using (await @lock.AcquireAsync())
        {
        }

        var collection = database.GetCollection<MongoLockDocument>(collectionName);
        using var cursor = await collection.Indexes.ListAsync();
        var indexes = await cursor.ToListAsync();
        
        var ttlIndex = indexes.FirstOrDefault(i => i["name"] == "expiresAt_ttl");
        Assert.That(ttlIndex, Is.Not.Null, "TTL index should exist");
        Assert.That(ttlIndex!["expireAfterSeconds"].AsInt32, Is.EqualTo(0)); // check functionality
    }

    [Test]
    [Category("CI")]
    public async Task TestIndexCreationIsScopedToCluster()
    {
        // Simulate two distinct databases with SAME name/collection but treated as different
        var db1 = new Mock<IMongoDatabase>(MockBehavior.Strict);
        var db2 = new Mock<IMongoDatabase>(MockBehavior.Strict);
        
        var coll1 = new Mock<IMongoCollection<MongoLockDocument>>(MockBehavior.Strict);
        var coll2 = new Mock<IMongoCollection<MongoLockDocument>>(MockBehavior.Strict);

        // We can't easily mock ClusterId equality without deeper mocking, 
        // but verify that if we use the *same* logic we rely on unique behavior?
        // Wait, unit testing static cache with mocks is tricky because state persists.
        // We need a unique db/coll name to avoid interference from other tests.
        var uniqueName = "db_" + Guid.NewGuid().ToString("N");
        SetDb(db1, coll1, uniqueName, "locks");
        SetDb(db2, coll2, uniqueName, "locks");

        // We want to verify ConfigureIndexes is called on BOTH.
        
        // Setup index creation mocks
        var idx1 = new Mock<IMongoIndexManager<MongoLockDocument>>(MockBehavior.Strict);
        var idx2 = new Mock<IMongoIndexManager<MongoLockDocument>>(MockBehavior.Strict);
        
        coll1.Setup(c => c.Indexes).Returns(idx1.Object);
        coll2.Setup(c => c.Indexes).Returns(idx2.Object);

        // Allow FindOneAndUpdate
        coll1.Setup(c => c.FindOneAndUpdateAsync(It.IsAny<FilterDefinition<MongoLockDocument>>(), It.IsAny<UpdateDefinition<MongoLockDocument>>(), It.IsAny<FindOneAndUpdateOptions<MongoLockDocument, MongoLockDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MongoLockDocument)null!);
        coll2.Setup(c => c.FindOneAndUpdateAsync(It.IsAny<FilterDefinition<MongoLockDocument>>(), It.IsAny<UpdateDefinition<MongoLockDocument>>(), It.IsAny<FindOneAndUpdateOptions<MongoLockDocument, MongoLockDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MongoLockDocument)null!);

        // Expect CreateOneAsync
        idx1.Setup(i => i.CreateOneAsync(It.IsAny<CreateIndexModel<MongoLockDocument>>(), It.IsAny<CreateOneIndexOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("idx");
        idx2.Setup(i => i.CreateOneAsync(It.IsAny<CreateIndexModel<MongoLockDocument>>(), It.IsAny<CreateOneIndexOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("idx");

        var lock1 = new MongoDistributedLock("k", db1.Object, "locks");
        var lock2 = new MongoDistributedLock("k", db2.Object, "locks");

        await lock1.TryAcquireAsync();
        await lock2.TryAcquireAsync();

        idx1.Verify(i => i.CreateOneAsync(It.IsAny<CreateIndexModel<MongoLockDocument>>(), It.IsAny<CreateOneIndexOptions>(), It.IsAny<CancellationToken>()), Times.Once, "First DB should create index");
        
        idx2.Verify(i => i.CreateOneAsync(It.IsAny<CreateIndexModel<MongoLockDocument>>(), It.IsAny<CreateOneIndexOptions>(), It.IsAny<CancellationToken>()), Times.Once, "Second DB should create index too because it's a different instance");
    }

    private static void SetDb(Mock<IMongoDatabase> db, Mock<IMongoCollection<MongoLockDocument>> coll, string dbName, string collName)
    {
        db.Setup(d => d.GetCollection<MongoLockDocument>(collName)).Returns(coll.Object);
        var dbNs = new DatabaseNamespace(dbName);
        var collNs = new CollectionNamespace(dbNs, collName);
        
        coll.Setup(c => c.CollectionNamespace).Returns(collNs);
        coll.Setup(c => c.Database).Returns(db.Object);
        db.Setup(d => d.DatabaseNamespace).Returns(dbNs);

        // Mock Client and Settings
        var client = new Mock<IMongoClient>(MockBehavior.Strict);
        // Ensure settings are distinct by adding a random server address
        var settings = new MongoClientSettings { Servers = [new("host" + Guid.NewGuid().ToString("N"))] };
        client.Setup(c => c.Settings).Returns(settings);
        db.Setup(d => d.Client).Returns(client.Object);
    }

    [Test]
    [Category("CI")]
    public async Task TestIndexCreationFailureIsCached()
    {
        var db = new Mock<IMongoDatabase>(MockBehavior.Strict);
        var coll = new Mock<IMongoCollection<MongoLockDocument>>(MockBehavior.Strict);
        SetDb(db, coll, "db_" + Guid.NewGuid().ToString("N"), "locks");

        var idx = new Mock<IMongoIndexManager<MongoLockDocument>>(MockBehavior.Strict);
        coll.Setup(c => c.Indexes).Returns(idx.Object);

        // Fail first time
        idx.SetupSequence(i => i.CreateOneAsync(It.IsAny<CreateIndexModel<MongoLockDocument>>(), It.IsAny<CreateOneIndexOptions>(), It.IsAny<CancellationToken>()))
           .ThrowsAsync(new MongoException("Test failure"))
           .ReturnsAsync("idx");

        // Allow FindOneAndUpdate
        coll.Setup(c => c.FindOneAndUpdateAsync(It.IsAny<FilterDefinition<MongoLockDocument>>(), It.IsAny<UpdateDefinition<MongoLockDocument>>(), It.IsAny<FindOneAndUpdateOptions<MongoLockDocument, MongoLockDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MongoLockDocument)null!);
        
        var @lock = new MongoDistributedLock("k", db.Object, "locks");
        
        // First acquire: fails to create index (swallowed), acquires lock
        await @lock.TryAcquireAsync();
        
        // Second acquire: should retry index creation if we fix it. 
        // Currently it caches the failed task, so it won't retry.
        await @lock.TryAcquireAsync();

        // Verify CreateOneAsync was called TWICE (proving retry).
        // If the current bug exists, it will be called ONCE.
        idx.Verify(i => i.CreateOneAsync(It.IsAny<CreateIndexModel<MongoLockDocument>>(), It.IsAny<CreateOneIndexOptions>(), It.IsAny<CancellationToken>()), Times.Exactly(2), "Should retry index creation after failure");
    }

    [Test]
    public async Task TestLockDocumentStructure()
    {
        var database = MongoDbCredentials.GetDefaultDatabase(Environment.CurrentDirectory);
        var lockName = TestHelper.UniqueName;
        var collectionName = "locks_" + Guid.NewGuid().ToString("N");
        
        var @lock = new MongoDistributedLock(lockName, database, collectionName, o => o.Expiry(TimeSpan.FromSeconds(10)));
        await using var handle = await @lock.AcquireAsync();
        var collection = database.GetCollection<MongoLockDocument>(collectionName);
        var doc = await collection.Find(Builders<MongoLockDocument>.Filter.Eq(d => d.Id, lockName)).FirstOrDefaultAsync();
            
        Assert.That(doc, Is.Not.Null);
        Assert.That(doc.LockId, Is.Not.Null);
        Assert.That(doc.FencingToken, Is.GreaterThan(0));
            
        // Allow for some clock skew/processing time.
        // Mongo and BsonDateTime usually assume UTC; check implicit assumption.
            
        // Depending on Mongo version and driver, dates are UTC.
        // The lock sets expiresAt = $$NOW + 10s.
        // Check that it is roughly in the future.
        Assert.That(doc.ExpiresAt.ToUniversalTime(), Is.GreaterThan(DateTime.UtcNow.AddSeconds(5)));
        Assert.That(doc.ExpiresAt.ToUniversalTime(), Is.LessThan(DateTime.UtcNow.AddSeconds(15)));
    }
}