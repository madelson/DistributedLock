using dotnet_etcd;
using dotnet_etcd.interfaces;
using Etcdserverpb;
using Grpc.Core;
using Medallion.Threading.Internal;
using V3Lockpb;

namespace Medallion.Threading.Etcd;

internal class EtcdClientWrapper 
{
    private readonly IEtcdClient _etcdClient;
    public EtcdClientWrapper(IEtcdClient etcdClient)
    {
        this._etcdClient = etcdClient ?? throw new ArgumentNullException(nameof(etcdClient));
    }

    public ValueTask<LeaseGrantResponse> LeaseGrantAsync(LeaseGrantRequest request, CancellationToken cancellationToken)
    {
        return SyncViaAsync.IsSynchronous
            ? new ValueTask<LeaseGrantResponse>(this._etcdClient.LeaseGrant(request,
                cancellationToken: cancellationToken))
            : new ValueTask<LeaseGrantResponse>(
                this._etcdClient.LeaseGrantAsync(request, cancellationToken: cancellationToken));
    }

    public Task LeaseKeepAliveAsync(long leaseId, CancellationToken token)
        => this._etcdClient.LeaseKeepAlive(leaseId, token);

    public async ValueTask<LockResponse> LockAsync(LockRequest lockRequest, CancellationToken cancellationToken)
    {
        var response = SyncViaAsync.IsSynchronous
            ? this._etcdClient.Lock(lockRequest, cancellationToken: cancellationToken)
            : await this._etcdClient.LockAsync(lockRequest, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (response == null)
        {
            throw new RpcException(new Status(StatusCode.Internal, "Lock failed"));
        }

        return response;
    }

    public async ValueTask LeaseRevokeAsync(LeaseRevokeRequest leaseRevokeRequest)
    {
        if (SyncViaAsync.IsSynchronous)
        {
            this._etcdClient.LeaseRevoke(leaseRevokeRequest);
        }
        else
        {
            await this._etcdClient.LeaseRevokeAsync(leaseRevokeRequest).ConfigureAwait(false);
        }
    }
}