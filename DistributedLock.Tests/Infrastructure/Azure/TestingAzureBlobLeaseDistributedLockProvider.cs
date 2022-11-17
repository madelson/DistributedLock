using Azure.Storage.Blobs;
using Medallion.Threading.Azure;

namespace Medallion.Threading.Tests.Azure;

public sealed class TestingAzureBlobLeaseDistributedLockProvider : TestingLockProvider<TestingAzureBlobLeaseSynchronizationStrategy>
{
    private readonly HashSet<Uri> _createdBlobs = new HashSet<Uri>();

    public override IDistributedLock CreateLockWithExactName(string name)
    {
        var client = new BlobClient(AzureCredentials.ConnectionString, this.Strategy.ContainerName, name);
        if (this.Strategy.CreateBlobBeforeLockIsCreated)
        {
            lock (this._createdBlobs)
            {
                if (this._createdBlobs.Add(client.Uri))
                {
                    client.Upload(Stream.Null);
                }
            }
        }
        return new AzureBlobLeaseDistributedLock(client, this.Strategy.Options);
    }

    public override string GetSafeName(string name) => 
        AzureBlobLeaseDistributedLock.GetSafeName(name, new BlobContainerClient(AzureCredentials.ConnectionString, this.Strategy.ContainerName));
}
