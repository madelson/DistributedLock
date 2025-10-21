using Medallion.Threading.MongoDB;
using MongoDB.Driver;
using NUnit.Framework;

namespace Medallion.Threading.Tests.MongoDB;

[Category("CI")]
public class MongoDistributedSynchronizationProviderTest
{
    [Test]
    public void TestArgumentValidation()
    {
        var database = MongoDbCredentials.GetDefaultDatabase(Environment.CurrentDirectory);
        Assert.Throws<ArgumentNullException>(() => new MongoDistributedSynchronizationProvider(null!));
        Assert.Throws<ArgumentNullException>(() => new MongoDistributedSynchronizationProvider(database, (string)null!));
        Assert.DoesNotThrow(() => new MongoDistributedSynchronizationProvider(database));
        Assert.DoesNotThrow(() => new MongoDistributedSynchronizationProvider(database, "CustomCollection"));
    }

    [Test]
    public void TestIDistributedLockProviderInterface()
    {
        var database = MongoDbCredentials.GetDefaultDatabase(Environment.CurrentDirectory);
        IDistributedLockProvider provider = new MongoDistributedSynchronizationProvider(database);
        var @lock = provider.CreateLock("interfaceTest");
        Assert.That(@lock, Is.Not.Null);
        Assert.That(@lock, Is.InstanceOf<MongoDistributedLock>());
        Assert.That(@lock.Name, Is.EqualTo("interfaceTest"));
    }

    [Test]
    public async Task TestProviderCreateLock()
    {
        var database = MongoDbCredentials.GetDefaultDatabase(Environment.CurrentDirectory);
        var provider = new MongoDistributedSynchronizationProvider(database);
        var lock1 = provider.CreateLock("testLock1");
        var lock2 = provider.CreateLock("testLock2");
        Assert.That(lock1, Is.Not.Null);
        Assert.That(lock2, Is.Not.Null);
        Assert.That(lock1.Name, Is.EqualTo("testLock1"));
        Assert.That(lock2.Name, Is.EqualTo("testLock2"));

        // Test that locks work
        await using (var handle1 = await lock1.AcquireAsync())
        await using (var handle2 = await lock2.AcquireAsync())
        {
            Assert.That(handle1, Is.Not.Null);
            Assert.That(handle2, Is.Not.Null);
        }
    }

    [Test]
    public async Task TestProviderWithCustomCollection()
    {
        var database = MongoDbCredentials.GetDefaultDatabase(Environment.CurrentDirectory);
        const string CustomCollection = "TestProviderLocks";
        var provider = new MongoDistributedSynchronizationProvider(database, CustomCollection);
        var @lock = provider.CreateLock("testLock");
        await using (var handle = await @lock.AcquireAsync())
        {
            Assert.That(handle, Is.Not.Null);
        }

        // Verify the custom collection was used
        var collectionExists = (await database.ListCollectionNamesAsync()).ToList().Contains(CustomCollection);
        Assert.That(collectionExists, Is.True);

        // Cleanup
        await database.DropCollectionAsync(CustomCollection);
    }
}