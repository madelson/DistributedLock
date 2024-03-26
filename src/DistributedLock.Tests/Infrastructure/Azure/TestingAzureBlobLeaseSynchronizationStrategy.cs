using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Medallion.Threading.Azure;
using NUnit.Framework;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Medallion.Threading.Tests.Azure;

public sealed class TestingAzureBlobLeaseSynchronizationStrategy : TestingSynchronizationStrategy
{
    private readonly DisposableCollection _disposables = new();

    private static readonly Action<AzureBlobLeaseOptionsBuilder> DefaultTestingOptions = o => 
        // for test speed
        o.BusyWaitSleepTime(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(25));

    public string ContainerName { get; set; } = AzureCredentials.DefaultBlobContainerName;
    
    public Action<AzureBlobLeaseOptionsBuilder>? Options { get; set; } = DefaultTestingOptions;
    public bool CreateBlobBeforeLockIsCreated { get; set; }

    public override IDisposable? PrepareForHandleLost()
    {
        this.Options = o =>
        {
            DefaultTestingOptions(o);
            o.RenewalCadence(TimeSpan.FromMilliseconds(10));
        };

        using var md5 = MD5.Create();
        this.ContainerName = $"distributed-lock-handle-lost-{new BigInteger(md5.ComputeHash(Encoding.UTF8.GetBytes(TargetFramework.Current + TestContext.CurrentContext.Test.FullName))):x}";
        var containerClient = new BlobContainerClient(AzureCredentials.ConnectionString, this.ContainerName);
        containerClient.CreateIfNotExists();
        this._disposables.Add(() => containerClient.DeleteIfExists());
        return new HandleLostScope(this.ContainerName);
    }

    public override void PrepareForHighContention(ref int maxConcurrentAcquires)
    {
        this.Options = null; // reduces # of requests under high contention
        this.CreateBlobBeforeLockIsCreated = true;
    }

    public override void Dispose()
    {
        try { this._disposables.Dispose(); }
        finally { base.Dispose(); }
    }

    private class HandleLostScope : IDisposable
    {
        private readonly string _containerName;

        public HandleLostScope(string containerName)
        {
            this._containerName = containerName;
        }

        public void Dispose()
        {
            var containerClient = new BlobContainerClient(AzureCredentials.ConnectionString, this._containerName);
            foreach (var blob in containerClient.GetBlobs())
            {
                var leaseClient = containerClient.GetBlobClient(blob.Name).GetBlobLeaseClient();
                leaseClient.Break(breakPeriod: TimeSpan.Zero);
            }
        }
    }
}
