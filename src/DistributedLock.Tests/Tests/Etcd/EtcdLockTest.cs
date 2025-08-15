using Medallion.Threading.Etcd;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Etcd;

[Category("CI")]
public class EtcdLockTest
{
    private readonly EtcdClusterSetup _etcdClusterBuilder = EtcdSetupFixture.EtcdClusterSetup;


    [Test]
    public void EtcdBasicAcquireLockSync_HappyPath()
    {
        using var client = this._etcdClusterBuilder.CreateClientToEtcdCluster();
        var lock2 = new EtcdLeaseDistributedLock(client, "etcd");
        using var handle2 = lock2.TryAcquire();
        Assert.That(handle2, Is.Not.Null, "Failed to acquire lock");
    }

    [Test]
    public async Task EtcdBasicAcquireLockAsync_HappyPath()
    {
        using var client = this._etcdClusterBuilder.CreateClientToEtcdCluster();
        var lock2 = new EtcdLeaseDistributedLock(client, "etcd");
        await using var handle2 = await lock2.TryAcquireAsync();
        Assert.That(handle2, Is.Not.Null, "Failed to acquire lock");
    }


    [Test]
    public async Task CreateLockAndAcquireInDifferentScope()
    {
        using var client = this._etcdClusterBuilder.CreateClientToEtcdCluster();

        async Task<IDistributedSynchronizationHandle> GetHandle()
        {
            var lock1 = new EtcdLeaseDistributedLock(client, "lock");
            return await lock1.TryAcquireAsync();
        }

        var handle = await GetHandle();
        handle.Dispose();
        // take the same lock again should be possible
        var lock2 = new EtcdLeaseDistributedLock(client, "lock");
        await using var handle2 = await lock2.TryAcquireAsync();
    }
}