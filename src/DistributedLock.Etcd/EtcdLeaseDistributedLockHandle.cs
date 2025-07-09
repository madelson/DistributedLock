using Etcdserverpb;
using Medallion.Threading.Internal;

namespace Medallion.Threading.Etcd;

public sealed class EtcdLeaseDistributedLockHandle : IDistributedSynchronizationHandle
{
    private readonly string _key;
    private readonly long _leaseKey;
    private readonly EtcdClientWrapper _client;

    internal EtcdLeaseDistributedLockHandle(string key, EtcdClientWrapper client, long leaseKey)
    {
        this._key = key ?? throw new ArgumentNullException(nameof(key));
        this._client = client ?? throw new ArgumentNullException(nameof(client));
        this._leaseKey = leaseKey;
        // Because this is a lease, managed finalization mostly won't be strictly necessary here. Where it comes in handy is:
        // (1) Ensuring blob deletion if we own the blob
        // (2) Helping release infinite-duration leases (rare case)
        // (3) In testing, avoiding having to wait 15+ seconds for lease expiration
    }


    /// <summary>
    /// Implements <see cref="IDistributedSynchronizationHandle.HandleLostToken"/>
    /// TODO implement HandleLostToken
    /// </summary>
    public CancellationToken HandleLostToken => new();


    /// <summary>
    /// Releases the lock
    /// </summary>
    public void Dispose() => this.DisposeSyncViaAsync();

    /// <summary>
    /// Releases the lock asynchronously
    /// </summary>
    public ValueTask DisposeAsync()
    {
        //TODO lease revoke will die here
        return this._client.LeaseRevokeAsync(new LeaseRevokeRequest { ID = this._leaseKey });
    }
}