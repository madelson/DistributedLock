using Medallion.Threading.Internal;
using Medallion.Threading.MongoDB;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;
using System.Text;

namespace Medallion.Threading.Tests.MongoDB;

public class MongoDistributedLockTest
{
    [Test]
    public async Task TestBasicLockFunctionality()
    {
        var database = MongoDBCredentials.GetDefaultDatabase(Environment.CurrentDirectory);
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

        // Make sure index was created
        var collection = database.GetCollection<MongoLockDocument>(MongoDistributedLock.DefaultCollectionName);
        await TestHelper.WaitForAsync(() => MongoIndexInitializer.CheckIfIndexExists(collection).AsValueTask(), TimeSpan.FromSeconds(15));
    }

    [Test]
    public async Task TestCustomCollectionName()
    {
        var database = MongoDBCredentials.GetDefaultDatabase(Environment.CurrentDirectory);
        var lockName = TestHelper.UniqueName;
        var customCollectionName = TestHelper.UniqueName + "-locks";
        var @lock = new MongoDistributedLock(lockName, database, customCollectionName);
        await using (var handle = await @lock.AcquireAsync())
        {
            Assert.That(handle, Is.Not.Null);
        }

        // Verify the collection was created
        var collectionExists = (await database.ListCollectionNamesAsync()).ToList().Contains(customCollectionName);
        Assert.That(collectionExists, Is.True);

        // Make sure index was created
        var collection = database.GetCollection<MongoLockDocument>(customCollectionName);
        await TestHelper.WaitForAsync(() => MongoIndexInitializer.CheckIfIndexExists(collection).AsValueTask(), TimeSpan.FromSeconds(15));

        // Cleanup
        await database.DropCollectionAsync(customCollectionName);
    }

    [Test]
    public async Task TestLockNameSupport()
    {
        using var provider = new TestingMongoDistributedLockProvider();

        var randomBytes = new byte[100_000];
        new Random(12345).NextBytes(randomBytes);
        var utf8 = Encoding.GetEncoding(
            "utf-8",
            new EncoderReplacementFallback("?"),
            new DecoderReplacementFallback("?"));
        var name = TestHelper.UniqueName + utf8.GetString(randomBytes);

        var @lock = provider.CreateLockWithExactName(name);
        await using var handle = await @lock.TryAcquireAsync();
        Assert.IsNotNull(handle);

        @lock = provider.CreateLockWithExactName(TestHelper.UniqueName + new string('z', 16_000_000));
        await using var handle2 = await @lock.TryAcquireAsync();
        Assert.IsNotNull(handle2);

        @lock = provider.CreateLockWithExactName("");
        await using var handle3 = await @lock.TryAcquireAsync();
        Assert.IsNotNull(handle3);

        @lock = provider.CreateLockWithExactName(" ");
        await using var handle4 = await @lock.TryAcquireAsync();
        Assert.IsNotNull(handle4);

        @lock = provider.CreateLockWithExactName("\0");
        await using var handle5 = await @lock.TryAcquireAsync();
        Assert.IsNotNull(handle5);
    }

    [Test]
    public async Task TestHandleLostToken()
    {
        var database = MongoDBCredentials.GetDefaultDatabase(Environment.CurrentDirectory);
        // Configure a short extension cadence so the test doesn't have to wait too long
        var @lock = new MongoDistributedLock(TestHelper.UniqueName, database, options: o => o.ExtensionCadence(TimeSpan.FromMilliseconds(500)));
        await using var handle = await @lock.AcquireAsync();
        Assert.That(handle, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(handle.HandleLostToken.CanBeCanceled, Is.True);
            Assert.That(handle.HandleLostToken.IsCancellationRequested, Is.False);
        });

        // Manually delete the lock document to simulate lock loss
        var collection = database.GetCollection<MongoLockDocument>(MongoDistributedLock.DefaultCollectionName);
        Assert.That((await collection.DeleteOneAsync(Builders<MongoLockDocument>.Filter.Eq(d => d.Id, @lock.Key))).DeletedCount, Is.EqualTo(1));

        // Wait a bit for the extension task to detect the loss
        var timeout = Task.Delay(TimeSpan.FromSeconds(4));
        while (!handle.HandleLostToken.IsCancellationRequested && !timeout.IsCompleted) { }
        Assert.That(handle.HandleLostToken.IsCancellationRequested, Is.True, "HandleLostToken should be signaled when lock is lost");
    }

    [Test]
    public async Task TestLockContentionAsync()
    {
        var database = MongoDBCredentials.GetDefaultDatabase(Environment.CurrentDirectory);
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
    public void TestValidatesConstructorParameters()
    {
        var database = new Mock<IMongoDatabase>(MockBehavior.Strict).Object;
        Assert.Throws<ArgumentNullException>(() => new MongoDistributedLock(null!, database));
        Assert.Throws<ArgumentNullException>(() => new MongoDistributedLock("key", null!));
        Assert.Throws<ArgumentNullException>(() => new MongoDistributedLock("key", database, (string)null!));
    }

    [Test]
    [Category("CI")]
    public void TestActivitySourceNaming()
    {
        var assemblyName = typeof(MongoDistributedLock).Assembly.GetName()!;
        Assert.That(MongoDistributedLock.ActivitySource.Name, Is.EqualTo(assemblyName.Name));
        Assert.That(MongoDistributedLock.ActivitySource.Version, Is.EqualTo(assemblyName.Version!.ToString(3)));
    }

    [Test]
    public async Task TestIndexExistence()
    {
        var database = MongoDBCredentials.GetDefaultDatabase(Environment.CurrentDirectory);
        var collectionName = "TestIndex" + Guid.NewGuid().ToString("N");
        
        var @lock = new MongoDistributedLock("lock", database, collectionName);
        await using (await @lock.AcquireAsync())
        {
        }

        var collection = database.GetCollection<MongoLockDocument>(collectionName);
        await TestHelper.WaitForAsync(async () =>
        {
            using var cursor = await collection.Indexes.ListAsync();
            var indexes = await cursor.ToListAsync();
            var ttlIndex = indexes.FirstOrDefault(i => i["name"] == "expiresAt_ttl");
            if (ttlIndex is null) { return false; }
            Assert.That(ttlIndex!["expireAfterSeconds"].AsInt32, Is.EqualTo(0)); // check functionality
            return true;
        }, TimeSpan.FromSeconds(15));
    }

    [Test]
    [Category("CI")]
    public async Task TestIndexCreationIsScopedToCluster()
    {
        // Simulate two distinct databases with SAME name/collection but treated as different
        var db1 = new Mock<IMongoDatabase>(MockBehavior.Strict);
        var db2 = new Mock<IMongoDatabase>(MockBehavior.Strict);
        
        var collection1 = new Mock<IMongoCollection<MongoLockDocument>>(MockBehavior.Strict);
        var collection2 = new Mock<IMongoCollection<MongoLockDocument>>(MockBehavior.Strict);

        // We can't easily mock ClusterId equality without deeper mocking, 
        // We need a unique db/coll name to avoid interference from other tests but we want
        // it to be the same otherwise.
        var uniqueName = "db_" + Guid.NewGuid().ToString("N");

        // Setup index creation mocks
        var index1 = new Mock<IMongoIndexManager<MongoLockDocument>>(MockBehavior.Strict);
        var index2 = new Mock<IMongoIndexManager<MongoLockDocument>>(MockBehavior.Strict);

        foreach (var (index, collection, db) in new[] { (index1, collection1, db1), (index2, collection2, db2) })
        {
            SetDb(db, collection, uniqueName, "locks");

            collection.Setup(c => c.Indexes).Returns(index.Object);

            // Expect CreateOneAsync
            index.Setup(i => i.CreateOneAsync(It.IsAny<CreateIndexModel<MongoLockDocument>>(), It.IsAny<CreateOneIndexOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("idx");

            var @lock = new MongoDistributedLock("k", db.Object, "locks");

            // First set it up so that acquire will fail
            collection.Setup(c => c.FindOneAndUpdateAsync(It.IsAny<FilterDefinition<MongoLockDocument>>(), It.IsAny<UpdateDefinition<MongoLockDocument>>(), It.IsAny<FindOneAndUpdateOptions<MongoLockDocument, MongoLockDocument>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((MongoLockDocument)null!);
            await @lock.TryAcquireAsync();
            index.Verify(i => i.CreateOneAsync(It.IsAny<CreateIndexModel<MongoLockDocument>>(), It.IsAny<CreateOneIndexOptions>(), It.IsAny<CancellationToken>()), Times.Never, "Failed acquire does not trigger index creation");

            // Allow FindOneAndUpdate
            collection.Setup(c => c.FindOneAndUpdateAsync(It.IsAny<FilterDefinition<MongoLockDocument>>(), It.IsAny<UpdateDefinition<MongoLockDocument>>(), It.IsAny<FindOneAndUpdateOptions<MongoLockDocument, MongoLockDocument>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((FilterDefinition<MongoLockDocument> filter, UpdateDefinition<MongoLockDocument> update, FindOneAndUpdateOptions<MongoLockDocument, MongoLockDocument> findAndUpdate, CancellationToken _) =>
                {
                    // this is reversing the construction in MongoDistributedLock.CreateAcquireUpdate()
                    var pipeline = (BsonDocumentStagePipelineDefinition<MongoLockDocument, MongoLockDocument>)((PipelineUpdateDefinition<MongoLockDocument>)update).Pipeline;
                    var lockId = pipeline.Documents[0]["$set"]["lockId"]["$cond"][1].AsString;
                    return new MongoLockDocument { Id = Guid.NewGuid().ToString(), LockId = lockId };
                });
            
            await @lock.TryAcquireAsync();
        }

        // We want to verify ConfigureIndexes is called on BOTH.
        index1.Verify(i => i.CreateOneAsync(It.IsAny<CreateIndexModel<MongoLockDocument>>(), It.IsAny<CreateOneIndexOptions>(), It.IsAny<CancellationToken>()), Times.Once, "First DB should create index");        
        index2.Verify(i => i.CreateOneAsync(It.IsAny<CreateIndexModel<MongoLockDocument>>(), It.IsAny<CreateOneIndexOptions>(), It.IsAny<CancellationToken>()), Times.Once, "Second DB should create index too because it's a different instance");
    }

    private static void SetDb(Mock<IMongoDatabase> db, Mock<IMongoCollection<MongoLockDocument>> coll, string dbName, string collName)
    {
        db.Setup(d => d.GetCollection<MongoLockDocument>(collName, null)).Returns(coll.Object);
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
        var collection = new Mock<IMongoCollection<MongoLockDocument>>(MockBehavior.Strict);
        SetDb(db, collection, "db_" + Guid.NewGuid().ToString("N"), "locks");

        var index = new Mock<IMongoIndexManager<MongoLockDocument>>(MockBehavior.Strict);
        collection.Setup(c => c.Indexes).Returns(index.Object);

        // Fail first time
        index.SetupSequence(i => i.CreateOneAsync(It.IsAny<CreateIndexModel<MongoLockDocument>>(), It.IsAny<CreateOneIndexOptions>(), It.IsAny<CancellationToken>()))
           .ThrowsAsync(new MongoException("Test failure"))
           .ReturnsAsync("idx");

        // Allow FindOneAndUpdate
        collection.Setup(c => c.FindOneAndUpdateAsync(It.IsAny<FilterDefinition<MongoLockDocument>>(), It.IsAny<UpdateDefinition<MongoLockDocument>>(), It.IsAny<FindOneAndUpdateOptions<MongoLockDocument, MongoLockDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FilterDefinition<MongoLockDocument> filter, UpdateDefinition<MongoLockDocument> update, FindOneAndUpdateOptions<MongoLockDocument, MongoLockDocument> findAndUpdate, CancellationToken _) =>
            {
                // this is reversing the construction in MongoDistributedLock.CreateAcquireUpdate()
                var pipeline = (BsonDocumentStagePipelineDefinition<MongoLockDocument, MongoLockDocument>)((PipelineUpdateDefinition<MongoLockDocument>)update).Pipeline;
                var lockId = pipeline.Documents[0]["$set"]["lockId"]["$cond"][1].AsString;
                return new MongoLockDocument { Id = Guid.NewGuid().ToString(), LockId = lockId };
            });

        var @lock = new MongoDistributedLock("k", db.Object, "locks");
        
        // First acquire: fails to create index (swallowed), acquires lock
        await @lock.TryAcquireAsync();
        
        // Second acquire: caches the failed task, so it won't retry.
        await @lock.TryAcquireAsync();

        // Verify CreateOneAsync was called ONCE (proving caching).
        index.Verify(i => i.CreateOneAsync(It.IsAny<CreateIndexModel<MongoLockDocument>>(), It.IsAny<CreateOneIndexOptions>(), It.IsAny<CancellationToken>()), Times.Once(), "Should retry index creation after failure");
    }

    [Test, Category("CI")]
    public async Task TestFailedIndexCreationEventuallyRetries()
    {
        var db = new Mock<IMongoDatabase>(MockBehavior.Strict);
        var collection = new Mock<IMongoCollection<MongoLockDocument>>(MockBehavior.Strict);
        SetDb(db, collection, "db_" + Guid.NewGuid().ToString("N"), "locks");

        var index = new Mock<IMongoIndexManager<MongoLockDocument>>(MockBehavior.Strict);
        collection.Setup(c => c.Indexes).Returns(index.Object);

        // Fail first time
        index.SetupSequence(i => i.CreateOneAsync(It.IsAny<CreateIndexModel<MongoLockDocument>>(), It.IsAny<CreateOneIndexOptions>(), It.IsAny<CancellationToken>()))
           .ThrowsAsync(new MongoException("Test failure"))
           .ReturnsAsync("idx");

        // Allow FindOneAndUpdate
        collection.Setup(c => c.FindOneAndUpdateAsync(It.IsAny<FilterDefinition<MongoLockDocument>>(), It.IsAny<UpdateDefinition<MongoLockDocument>>(), It.IsAny<FindOneAndUpdateOptions<MongoLockDocument, MongoLockDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FilterDefinition<MongoLockDocument> filter, UpdateDefinition<MongoLockDocument> update, FindOneAndUpdateOptions<MongoLockDocument, MongoLockDocument> findAndUpdate, CancellationToken _) =>
            {
                // this is reversing the construction in MongoDistributedLock.CreateAcquireUpdate()
                var pipeline = (BsonDocumentStagePipelineDefinition<MongoLockDocument, MongoLockDocument>)((PipelineUpdateDefinition<MongoLockDocument>)update).Pipeline;
                var lockId = pipeline.Documents[0]["$set"]["lockId"]["$cond"][1].AsString;
                return new MongoLockDocument { Id = Guid.NewGuid().ToString(), LockId = lockId };
            });

        Mock<MongoIndexInitializer> initializer = new();
        // set cache time to 0
        initializer.Setup(i => i.DelayBeforeRetry()).Returns(Task.CompletedTask);

        // First acquire: fails to create index (swallowed), acquires lock
        await initializer.Object.InitializeTtlIndex(collection.Object);

        // Second acquire: should retry index creation if we fix it. 
        // Currently it caches the failed task, so it won't retry.
        await initializer.Object.InitializeTtlIndex(collection.Object);

        // Verify CreateOneAsync was called TWICE (proving retry).
        index.Verify(i => i.CreateOneAsync(It.IsAny<CreateIndexModel<MongoLockDocument>>(), It.IsAny<CreateOneIndexOptions>(), It.IsAny<CancellationToken>()), Times.Exactly(2), "Should retry index creation after failure");
    }

    [Test]
    public async Task TestLockDocumentStructure()
    {
        var database = MongoDBCredentials.GetDefaultDatabase(Environment.CurrentDirectory);
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