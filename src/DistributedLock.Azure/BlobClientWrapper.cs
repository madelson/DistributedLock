using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Medallion.Threading.Internal;

namespace Medallion.Threading.Azure;

/// <summary>
/// Adds <see cref="SyncViaAsync"/> support to <see cref="BlobBaseClient"/>
/// </summary>
internal class BlobClientWrapper
{
    private readonly BlobBaseClient _blobClient;

    public BlobClientWrapper(BlobBaseClient blobClient)
    {
        this._blobClient = blobClient;
    }

    public string Name => this._blobClient.Name;

    public BlobLeaseClientWrapper GetBlobLeaseClient() => new BlobLeaseClientWrapper(this._blobClient.GetBlobLeaseClient());

    public async ValueTask<IDictionary<string, string>> GetMetadataAsync(string leaseId, CancellationToken cancellationToken)
    {
        var conditions = new BlobRequestConditions { LeaseId = leaseId };
        var properties = SyncViaAsync.IsSynchronous
            ? this._blobClient.GetProperties(conditions, cancellationToken)
            : await this._blobClient.GetPropertiesAsync(conditions, cancellationToken).ConfigureAwait(false);
        return properties.Value.Metadata;
    }

    public ValueTask CreateIfNotExistsAsync(IDictionary<string, string> metadata, CancellationToken cancellationToken)
    {
        switch (this._blobClient)
        {
            case BlobClient blobClient:
                if (SyncViaAsync.IsSynchronous)
                {
                    blobClient.Upload(Stream.Null, metadata: metadata, cancellationToken: cancellationToken);
                    return default;
                }
                return new ValueTask(blobClient.UploadAsync(Stream.Null, metadata: metadata, cancellationToken: cancellationToken));
            case BlockBlobClient blockBlobClient:
                if (SyncViaAsync.IsSynchronous)
                {
                    blockBlobClient.Upload(Stream.Null, metadata: metadata, cancellationToken: cancellationToken);
                    return default;
                }
                return new ValueTask(blockBlobClient.UploadAsync(Stream.Null, metadata: metadata, cancellationToken: cancellationToken));
            case PageBlobClient pageBlobClient:
                if (SyncViaAsync.IsSynchronous)
                {
                    pageBlobClient.CreateIfNotExists(size: 0, metadata: metadata, cancellationToken: cancellationToken);
                    return default;
                }
                return new ValueTask(pageBlobClient.CreateIfNotExistsAsync(size: 0, metadata: metadata, cancellationToken: cancellationToken));
            case AppendBlobClient appendBlobClient:
                if (SyncViaAsync.IsSynchronous)
                {
                    appendBlobClient.CreateIfNotExists(metadata: metadata, cancellationToken: cancellationToken);
                    return default;
                }
                return new ValueTask(appendBlobClient.CreateIfNotExistsAsync(metadata: metadata, cancellationToken: cancellationToken));
            default:
                throw new InvalidOperationException(
                    this._blobClient.GetType() == typeof(BlobBaseClient)
                        ? $"Unable to create a lock blob given client type {typeof(BlobBaseClient)}. Either ensure that the blob exists or use a non-base client type such as {typeof(BlobClient)}"
                            + " which specifies the type of blob to create"
                        : $"Unexpected blob client type {this._blobClient.GetType()}"
                );
        }
    }

    public ValueTask DeleteIfExistsAsync(string? leaseId = null)
    {
        var conditions = leaseId != null ? new BlobRequestConditions { LeaseId = leaseId } : null;
        if (SyncViaAsync.IsSynchronous)
        {
            this._blobClient.DeleteIfExists(conditions: conditions);
            return default;
        }
        return new ValueTask(this._blobClient.DeleteIfExistsAsync(conditions: conditions));
    }
}
