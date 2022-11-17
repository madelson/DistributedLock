using Azure.Storage.Blobs.Specialized;
using Medallion.Threading.Internal;

namespace Medallion.Threading.Azure;

/// <summary>
/// Adds <see cref="SyncViaAsync"/> support to <see cref="BlobLeaseClient"/>
/// </summary>
internal sealed class BlobLeaseClientWrapper
{
    private readonly BlobLeaseClient _blobLeaseClient;

    public BlobLeaseClientWrapper(BlobLeaseClient blobLeaseClient)
    {
        this._blobLeaseClient = blobLeaseClient;
    }

    public string LeaseId => this._blobLeaseClient.LeaseId;

    public ValueTask AcquireAsync(TimeoutValue duration, CancellationToken cancellationToken)
    {
        if (SyncViaAsync.IsSynchronous)
        {
            this._blobLeaseClient.Acquire(duration.TimeSpan, cancellationToken: cancellationToken);
            return default;
        }
        return new ValueTask(this._blobLeaseClient.AcquireAsync(duration.TimeSpan, cancellationToken: cancellationToken));
    }

    public ValueTask RenewAsync(CancellationToken cancellationToken)
    {
        if (SyncViaAsync.IsSynchronous)
        {
            this._blobLeaseClient.Renew(cancellationToken: cancellationToken);
            return default;
        }
        return new ValueTask(this._blobLeaseClient.RenewAsync(cancellationToken: cancellationToken));
    }

    public ValueTask ReleaseAsync()
    {
        if (SyncViaAsync.IsSynchronous)
        {
            this._blobLeaseClient.Release();
            return default;
        }
        return new ValueTask(this._blobLeaseClient.ReleaseAsync());
    }
}
