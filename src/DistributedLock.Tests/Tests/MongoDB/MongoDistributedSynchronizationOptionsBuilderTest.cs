using Medallion.Threading.MongoDB;
using NUnit.Framework;

namespace Medallion.Threading.Tests.MongoDB;

public class MongoDistributedSynchronizationOptionsBuilderTest
{
    [Test]
    public void TestBusyWaitSleepTimeValidation()
    {
        var database = MongoDBCredentials.GetDefaultDatabase(Environment.CurrentDirectory);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MongoDistributedLock("test", database, options => options
                .BusyWaitSleepTime(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(500))));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MongoDistributedLock("test", database, options => options
                .BusyWaitSleepTime(Timeout.InfiniteTimeSpan, TimeSpan.FromSeconds(1))));
        Assert.DoesNotThrow(() =>
            new MongoDistributedLock("test", database, options => options
                .BusyWaitSleepTime(TimeSpan.FromMilliseconds(10), TimeSpan.FromSeconds(1))));
    }

    [Test]
    public void TestExpiryValidation()
    {
        var database = MongoDBCredentials.GetDefaultDatabase(Environment.CurrentDirectory);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MongoDistributedLock("test", database, options => options.Expiry(TimeSpan.FromMilliseconds(50))));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MongoDistributedLock("test", database, options => options.Expiry(Timeout.InfiniteTimeSpan)));
        Assert.DoesNotThrow(() =>
            new MongoDistributedLock("test", database, options => options.Expiry(TimeSpan.FromSeconds(1))));
    }

    [Test]
    public void TestExtensionCadenceValidation()
    {
        var database = MongoDBCredentials.GetDefaultDatabase(Environment.CurrentDirectory);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MongoDistributedLock("test", database, options => options
                                                                  .Expiry(TimeSpan.FromSeconds(5))
                                                                  .ExtensionCadence(TimeSpan.FromSeconds(10))));
        Assert.DoesNotThrow(() =>
            new MongoDistributedLock("test", database, options => options
                                                                  .Expiry(TimeSpan.FromSeconds(10))
                                                                  .ExtensionCadence(TimeSpan.FromSeconds(3))));
    }

    [Test]
    public async Task TestOptionsAreApplied()
    {
        var database = MongoDBCredentials.GetDefaultDatabase(Environment.CurrentDirectory);
        var lockName = TestHelper.UniqueName;
        var @lock = new MongoDistributedLock(lockName, database, options => options
                                                                            .Expiry(TimeSpan.FromSeconds(60))
                                                                            .ExtensionCadence(TimeSpan.FromSeconds(20))
                                                                            .BusyWaitSleepTime(TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(100)));
        await using (var handle = await @lock.AcquireAsync())
        {
            Assert.That(handle, Is.Not.Null);
        }
    }
}