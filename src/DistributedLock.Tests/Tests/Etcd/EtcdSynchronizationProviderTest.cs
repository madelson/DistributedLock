using Medallion.Threading.Etcd;
using Medallion.Threading.FileSystem;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Etcd;

public class EtcdSynchronizationProviderTest
{

    private readonly EtcdClusterSetup _etcdClusterBuilder = EtcdSetupFixture.EtcdClusterSetup;


    [Test]
    public void TestArgumentValidation()
    {
        Assert.Throws<ArgumentNullException>(() => new EtcdLeaseDistributedLockProvider(null!));
    }


    [Test]
    public async Task BasicTest()
    {
        var provider = new EtcdLeaseDistributedLockProvider(this._etcdClusterBuilder.CreateClientToEtcdCluster());
        var lock1 = provider.CreateLock("lockTest");
        await using var handle1 = await lock1.TryAcquireAsync();
        Assert.That(handle1, Is.Not.Null);
    }
}
