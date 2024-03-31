using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Medallion.Threading.Azure;
using Medallion.Threading.Internal;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Azure;

public class AzureBlobLeaseDistributedLockTest
{
    [Test]
    public void TestSafeNaming()
    {
        var names = new[]
        {
            string.Empty,
            new string('a', 2000),
            "a/",
            "a\\",
            string.Join("/", Enumerable.Repeat("a", 254)),
            string.Join(@"\", Enumerable.Repeat("b", 254)),
            new string('/', 254),
            new string('\\', 254)
        };

        var containerClient = new BlobContainerClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName);
        foreach (var name in names)
        {
            var @lock = new AzureBlobLeaseDistributedLock(containerClient, name);
            Assert.DoesNotThrow(() => @lock.Acquire());
        }
    }

    [Test]
    public async Task TestLockOnDifferentBlobClientTypes(
        [Values] BlobClientType type,
        [Values] bool isAsync)
    {
        if (isAsync)
        {
            await TestAsync();
        }
        else
        {
            SyncViaAsync.Run(_ => TestAsync(), default(object));
        }

        async ValueTask TestAsync()
        {
            using var provider = new TestingAzureBlobLeaseDistributedLockProvider();
            var name = provider.GetUniqueSafeName();
            var client = CreateClient(type, name);

            if (client.GetType() == typeof(BlobBaseClient))
            {
                // work around inability to do CreateIfNotExists for the base client
                await new BlobClientWrapper(new BlobClient(AzureCredentials.ConnectionString, client.BlobContainerName, client.Name))
                    .CreateIfNotExistsAsync(new Dictionary<string, string>(), CancellationToken.None);
            }

            var @lock = new AzureBlobLeaseDistributedLock(client);
            await using var handle = await @lock.TryAcquireAsync();
            Assert.IsNotNull(handle);
            await using var nestedHandle = await @lock.TryAcquireAsync();
            Assert.IsNull(nestedHandle);
        }
    }

    [Test]
    public async Task TestWrapperCreateIfNotExists([Values] BlobClientType type)
    {
        using var provider = new TestingAzureBlobLeaseDistributedLockProvider();
        var name = provider.GetUniqueSafeName();
        var client = CreateClient(type, name);
        var wrapper = new BlobClientWrapper(client);

        var metadata = new Dictionary<string, string> { ["abc"] = "123" };

        if (client.GetType() == typeof(BlobBaseClient))
        {
            Assert.That(
                Assert.ThrowsAsync<InvalidOperationException>(async () => await wrapper.CreateIfNotExistsAsync(metadata, CancellationToken.None))!.ToString(),
                Does.Contain("Either ensure that the blob exists or use a non-base client type")
            );
            return;
        }

        await wrapper.CreateIfNotExistsAsync(metadata, CancellationToken.None);
        Assert.IsTrue((await client.ExistsAsync()).Value);
        Assert.That((await client.GetPropertiesAsync()).Value.Metadata, Is.EqualTo(metadata).AsCollection);

        Assert.DoesNotThrowAsync(async () => await wrapper.CreateIfNotExistsAsync(metadata, CancellationToken.None));
        Assert.IsTrue((await client.ExistsAsync()).Value);
    }

    [Test]
    public void TestCanUseLeaseIdForBlobOperations()
    {
        using var provider = new TestingAzureBlobLeaseDistributedLockProvider();
        var name = provider.GetUniqueSafeName();
        var client = new PageBlobClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName, name);
        const int BlobSize = 512;
        client.Create(size: BlobSize);
        var @lock = new AzureBlobLeaseDistributedLock(client);

        using var handle = @lock.Acquire();
        Assert.Throws<RequestFailedException>(() => client.UploadPages(new MemoryStream(new byte[BlobSize]), offset: 0))!
            .ErrorCode.ShouldEqual(AzureErrors.LeaseIdMissing);

        Assert.DoesNotThrow(
            () => client.UploadPages(new MemoryStream(new byte[BlobSize]), offset: 0, options: new()
            {
                Conditions = new PageBlobRequestConditions { LeaseId = handle.LeaseId }
            })
        );

        handle.Dispose();
        Assert.Throws<ObjectDisposedException>(() => handle.LeaseId.ToString());
    }

    [Test]
    public void TestThrowsIfContainerDoesNotExist()
    {
        using var provider = new TestingAzureBlobLeaseDistributedLockProvider();
        provider.Strategy.ContainerName = "does-not-exist";
        var @lock = provider.CreateLock(nameof(TestThrowsIfContainerDoesNotExist));

        Assert.Throws<RequestFailedException>(() => @lock.TryAcquire()?.Dispose())!
            .ErrorCode.ShouldEqual("ContainerNotFound");
    }

    [Test]
    public void TestCanAcquireIfContainerLeased()
    {
        using var provider = new TestingAzureBlobLeaseDistributedLockProvider();
        provider.Strategy.ContainerName = "leased-container" + TargetFramework.Current.Replace('.', '-');

        var containerClient = new BlobContainerClient(AzureCredentials.ConnectionString, provider.Strategy.ContainerName);
        var containerLeaseClient = new BlobLeaseClient(containerClient);
        try
        {
            containerClient.CreateIfNotExists();
            containerLeaseClient.Acquire(TimeSpan.FromSeconds(60));

            var @lock = provider.CreateLock(nameof(TestCanAcquireIfContainerLeased));

            using var handle = @lock.TryAcquire();
            Assert.IsNotNull(handle);
        }
        finally
        {
            try { containerLeaseClient.Release(); }
            finally { containerClient.DeleteIfExists(); } 
        }
    }

    [Test]
    public async Task TestSuccessfulRenewal()
    {
        using var provider = new TestingAzureBlobLeaseDistributedLockProvider();
        provider.Strategy.Options = o => o.RenewalCadence(TimeSpan.FromSeconds(.05));
        var @lock = provider.CreateLock(nameof(TestSuccessfulRenewal));

        using var handle = @lock.Acquire();
        await Task.Delay(TimeSpan.FromSeconds(.2)); // long enough for renewal to run
        Assert.DoesNotThrow(handle.Dispose); // observes the result of the renewal task
    }

    [Test]
    [NonParallelizable, Retry(tryCount: 3)] // timing-sensitive
    public void TestTriggersHandleLostIfLeaseExpiresNaturally()
    {
        using var provider = new TestingAzureBlobLeaseDistributedLockProvider();
        provider.Strategy.Options = o => o.RenewalCadence(Timeout.InfiniteTimeSpan).Duration(TimeSpan.FromSeconds(15));
        var @lock = provider.CreateLock(nameof(TestTriggersHandleLostIfLeaseExpiresNaturally));

        using var handle = @lock.Acquire();
        using var @event = new ManualResetEventSlim(initialState: false);
        using var registration = handle.HandleLostToken.Register(@event.Set);
        using var faultingRegistration = handle.HandleLostToken.Register(() => throw new TimeZoneNotFoundException());

        Assert.IsTrue(@event.Wait(TimeSpan.FromSeconds(15.1)));

        Assert.Throws<RequestFailedException>(handle.Dispose)!
            .ErrorCode.ShouldEqual("LeaseNotPresentWithBlobOperation");
    }

    [Test]
    public void TestExitsDespiteLongSleepTime()
    {
        using var provider = new TestingAzureBlobLeaseDistributedLockProvider();
        provider.Strategy.Options = o => o.BusyWaitSleepTime(TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1));
        var @lock = provider.CreateLock(nameof(TestExitsDespiteLongSleepTime));

        using var handle1 = @lock.Acquire();

        var handle2Task = @lock.TryAcquireAsync(TimeSpan.FromSeconds(2)).AsTask();
        Assert.IsFalse(handle2Task.Wait(TimeSpan.FromSeconds(.05)));

        handle1.Dispose();
        Assert.IsTrue(handle2Task.Wait(TimeSpan.FromSeconds(5)));
    }

    private static BlobBaseClient CreateClient([Values] BlobClientType type, string name) => type switch
    {
        BlobClientType.Basic => new BlobClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName, name),
        BlobClientType.Block => new BlockBlobClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName, name),
        BlobClientType.Page => new PageBlobClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName, name),
        BlobClientType.Append => new AppendBlobClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName, name),
        BlobClientType.Base => new BlobBaseClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName, name),
        _ => throw new ArgumentException("Bad type", nameof(type)),
    };

    public enum BlobClientType
    {
        Basic,
        Block,
        Page,
        Append,
        Base
    }
}
