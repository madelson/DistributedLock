using Azure;
using Azure.Storage.Blobs.Specialized;
using Medallion.Threading.Internal;

namespace Medallion.Threading.Azure;

/// <summary>
/// Adds <see cref="SyncViaAsync"/> support to <see cref="BlobLeaseClient"/>
/// </summary>
internal sealed class BlobLeaseClientWrapper(BlobLeaseClient blobLeaseClient)
{
    public string LeaseId => blobLeaseClient.LeaseId;

    public ValueTask<Response> AcquireAsync(TimeoutValue duration, CancellationToken cancellationToken)
    {
        RequestContext requestContext = new()
        {
            CancellationToken = cancellationToken,
            ErrorOptions = ErrorOptions.NoThrow
        };

        return SyncViaAsync.IsSynchronous
            ? new ValueTask<Response>(blobLeaseClient.Acquire(duration.TimeSpan, conditions: null, requestContext))
            : new ValueTask<Response>(blobLeaseClient.AcquireAsync(duration.TimeSpan, conditions: null, requestContext));
    }

    public ValueTask RenewAsync(CancellationToken cancellationToken)
    {
        if (SyncViaAsync.IsSynchronous)
        {
            blobLeaseClient.Renew(cancellationToken: cancellationToken);
            return default;
        }
        return new ValueTask(blobLeaseClient.RenewAsync(cancellationToken: cancellationToken));
    }

    public ValueTask ReleaseAsync()
    {
        if (SyncViaAsync.IsSynchronous)
        {
            blobLeaseClient.Release();
            return default;
        }
        return new ValueTask(blobLeaseClient.ReleaseAsync());
    }
}