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
        var collection = database.GetCollection<MongoLockDocument>("DistributedLocks");
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
        @lock.Name.ShouldEqual(Name);
        @lock.Key.ShouldEqual(Name);
    }

    [Test]
    [Category("CI")]
    public void TestValidatesConstructorParameters()
    {
        var database = new Mock<IMongoDatabase>(MockBehavior.Strict).Object;
        Assert.Throws<ArgumentNullException>(() => new MongoDistributedLock(null!, database));
        Assert.Throws<ArgumentNullException>(() => new MongoDistributedLock(string.Empty, database));
        Assert.Throws<ArgumentNullException>(() => new MongoDistributedLock("key", null!));
        Assert.Throws<ArgumentNullException>(() => new MongoDistributedLock("key", database, (string)null!));
    }
}