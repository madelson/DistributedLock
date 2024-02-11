using Azure.Storage.Blobs;

namespace Medallion.Threading.Azure;

/// <summary>
/// Implements <see cref="IDistributedLockProvider"/> for <see cref="AzureBlobLeaseDistributedLock"/>
/// </summary>
public sealed class AzureBlobLeaseDistributedSynchronizationProvider : IDistributedLockProvider
{
    private readonly BlobContainerClient _blobContainerClient;
    private readonly Action<AzureBlobLeaseOptionsBuilder>? _options;

    /// <summary>
    /// Constructs a provider that scopes blobs within the provided <paramref name="blobContainerClient"/> and uses the provided <paramref name="options"/>.
    /// </summary>
    public AzureBlobLeaseDistributedSynchronizationProvider(BlobContainerClient blobContainerClient, Action<AzureBlobLeaseOptionsBuilder>? options = null)
    {
        this._blobContainerClient = blobContainerClient ?? throw new ArgumentNullException(nameof(blobContainerClient));
        this._options = options;
    }

    /// <summary>
    /// Constructs an <see cref="AzureBlobLeaseDistributedLock"/> with the given <paramref name="name"/>.
    /// </summary>
    public AzureBlobLeaseDistributedLock CreateLock(string name) => new AzureBlobLeaseDistributedLock(this._blobContainerClient, name, this._options);

    IDistributedLock IDistributedLockProvider.CreateLock(string name) => this.CreateLock(name);
}
