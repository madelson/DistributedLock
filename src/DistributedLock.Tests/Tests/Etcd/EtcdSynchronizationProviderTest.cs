using Medallion.Threading.Etcd;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Etcd;

public class EtcdSynchronizationProviderTest
{

    private readonly EtcdClusterSetup _etcdClusterSetup = new();

    [Test]
    public async Task BasicTest()
    {
        var provider = new EtcdLeaseDistributedLockProvider(this._etcdClusterSetup.CreateClientToEtcdCluster());
        var lock1 = provider.CreateLock("lockTest");
        await using var handle1 = await lock1.TryAcquireAsync();
        Assert.That(handle1, Is.Not.Null);
    }
}
