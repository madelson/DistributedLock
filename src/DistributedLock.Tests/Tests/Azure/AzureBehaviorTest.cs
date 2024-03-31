using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Medallion.Threading.Azure;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Azure;

/// <summary>
/// Demonstrates various behaviors of Azure blob storage that our implementation relies upon or takes into account
/// </summary>
public class AzureBehaviorTest
{
    [Test]
    public void TestAttemptToLeaseBlobIfDoesNotExist()
    {
        var blobClient = new BlobClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName, Guid.NewGuid().ToString());
        
        Assert.Throws<RequestFailedException>(() => blobClient.GetBlobLeaseClient().Acquire(TimeSpan.FromMinutes(1)))!
            .ErrorCode.ShouldEqual(AzureErrors.BlobNotFound);

        blobClient = new BlobClient(AzureCredentials.ConnectionString, "dne-container", Guid.NewGuid().ToString());
        Assert.Throws<RequestFailedException>(() => blobClient.GetBlobLeaseClient().Acquire(TimeSpan.FromMinutes(1)))!
            .ErrorCode.ShouldEqual("ContainerNotFound");
    }

    [Test]
    public void TestSlashEquivalence()
    {
        var name = Guid.NewGuid() + "/a";

        var blobClient1 = new BlobClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName, name);
        Assert.That((bool)blobClient1.Exists(), Is.False);
        blobClient1.Upload(Stream.Null);
        Assert.That((bool)blobClient1.Exists(), Is.True);

        var blobClient2 = new BlobClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName, name.Replace('/', '\\'));
        Assert.That((bool)blobClient2.Exists(), Is.True);
    }

    [Test]
    public void TestThrowsIfLeaseAlreadyHeld()
    {
        var name = nameof(TestThrowsIfLeaseAlreadyHeld) + Guid.NewGuid();
        var client1 = new BlobClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName, name);
        client1.Upload(Stream.Null);
        var client2 = new BlobClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName, name);
        Assert.DoesNotThrow(() => client1.GetBlobLeaseClient().Acquire(TimeSpan.FromSeconds(15)));
        Assert.Throws<RequestFailedException>(() => client2.GetBlobLeaseClient().Acquire(TimeSpan.FromSeconds(15)))!
            .ErrorCode.ShouldEqual("LeaseAlreadyPresent");
    }
}
