using dotnet_etcd.interfaces;

namespace Medallion.Threading.Etcd;

/// <summary>
/// Implements <see cref="IDistributedLockProvider"/> for <see cref="AzureBlobLeaseDistributedLock"/>
/// </summary>
public sealed class EtcdLeaseDistributedLockProvider : IDistributedLockProvider
{
    private readonly IEtcdClient _blobContainerClient;
    private readonly Action<EtcdLeaseOptionsBuilder>? _options;

    /// <summary>
    /// Constructs a provider that scopes blobs within the provided <paramref name="blobContainerClient"/> and uses the provided <paramref name="options"/>.
    /// </summary>
    public EtcdLeaseDistributedLockProvider(IEtcdClient blobContainerClient,
        Action<EtcdLeaseOptionsBuilder>? options = null)
    {
        this._blobContainerClient = blobContainerClient ?? throw new ArgumentNullException(nameof(blobContainerClient));
        this._options = options;
    }

    /// <summary>
    /// Constructs an <see cref="AzureBlobLeaseDistributedLock"/> with the given <paramref name="name"/>.
    /// </summary>
    public EtcdLeaseDistributedLock CreateLock(string name) => new(this._blobContainerClient, name, this._options);

    IDistributedLock IDistributedLockProvider.CreateLock(string name) => this.CreateLock(name);
}