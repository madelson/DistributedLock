using dotnet_etcd.interfaces;
using Etcdserverpb;
using Medallion.Threading.Internal;
using V3Lockpb;

namespace Medallion.Threading.Etcd;

/// <summary>
/// A distributed lock based on holding an exclusive handle to a lock file. The file will be deleted when the lock is released.
/// </summary>
public sealed partial class EtcdLeaseDistributedLock : IInternalDistributedLock<EtcdLeaseDistributedLockHandle>
{
    private readonly EtcdClientWrapper _etcdClient;

    private readonly (TimeoutValue duration, TimeoutValue renewalCadence, TimeoutValue minBusyWaitSleepTime,
        TimeoutValue maxBusyWaitSleepTime) _options;

    public EtcdLeaseDistributedLock(IEtcdClient client, string lockName,
        Action<EtcdLeaseOptionsBuilder>? options = null)
    {
        // TODO must support all non-null lock name via getsafename
        this.Name = lockName ?? throw new ArgumentNullException(nameof(lockName));
        if (lockName.Length == 0) { throw new FormatException($"{nameof(lockName)}: may not have an empty file name"); }

        this._etcdClient = new EtcdClientWrapper(client);
        this._options = EtcdLeaseOptionsBuilder.GetOptions(options);
    }

    // todo revisit API
    /// <summary>
    /// Implements <see cref="IDistributedLock.Name"/>
    /// </summary>
    public string Name { get; }


    ValueTask<EtcdLeaseDistributedLockHandle?> IInternalDistributedLock<EtcdLeaseDistributedLockHandle>.
        InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken) =>
        BusyWaitHelper.WaitAsync(
            state: this,
            tryGetValue: (@this, token) => @this.TryAcquireAsync(token),
            timeout: timeout,
            minSleepTime: this._options.minBusyWaitSleepTime,
            maxSleepTime: this._options.maxBusyWaitSleepTime,
            cancellationToken
        );

    private async ValueTask<EtcdLeaseDistributedLockHandle?> TryAcquireAsync(CancellationToken cancellationToken)
    {
        // TODO implement LeaseHandle
        cancellationToken.ThrowIfCancellationRequested();
        // TODO renewnal cadence should not be here
        var leaseResponse = await this._etcdClient.LeaseGrantAsync(
            new LeaseGrantRequest { TTL = this._options.renewalCadence.InSeconds },
            cancellationToken: cancellationToken).ConfigureAwait(false);
        var leaseId = leaseResponse.ID;
        var cancellationTokenSource = new CancellationTokenSource();
        _ = this._etcdClient.LeaseKeepAliveAsync(leaseId, cancellationTokenSource.Token).ConfigureAwait(false);
        var response =
            await this._etcdClient.LockAsync(
                new LockRequest { Name = Google.Protobuf.ByteString.CopyFromUtf8(this.Name), Lease = leaseId, },
                cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);
        var actualKey = response.Key;
        return new EtcdLeaseDistributedLockHandle(actualKey.ToString(), this._etcdClient, leaseId);
    }


    private static string GetSafeName(string name)
    {
        // TODO figure
        return DistributedLockHelpers.ToSafeName(name, 1000, s => s);
    }
}