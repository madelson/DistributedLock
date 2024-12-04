using Azure;
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

    public ValueTask<Response> AcquireAsync(TimeoutValue duration, CancellationToken cancellationToken)
    {
        var requestContext = new RequestContext
        {
            CancellationToken = cancellationToken,
            ErrorOptions = ErrorOptions.NoThrow
        };

        if (SyncViaAsync.IsSynchronous)
        {
            return new ValueTask<Response>(this._blobLeaseClient.Acquire(duration.TimeSpan, conditions: null, requestContext));
        }
        return new ValueTask<Response>(this._blobLeaseClient.AcquireAsync(duration.TimeSpan, conditions: null, requestContext));
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