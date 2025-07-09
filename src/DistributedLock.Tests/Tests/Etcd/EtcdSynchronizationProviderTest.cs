using Medallion.Threading.Etcd;
using Medallion.Threading.Tests.Etcd;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Etcd;


[Category("CI")]
public class EtcdSynchronizationProviderTest
{

    private readonly EtcdClusterSetup _etcdClusterSetup = new();

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await this._etcdClusterSetup.ClusterSetup();
    } 


    [Test]
    public async Task BasicTest()
    {
        var provider = new EtcdLeaseDistributedLockProvider(this._etcdClusterSetup.CreateClientToEtcdCluster());
        var lock1 = provider.CreateLock("lockTest");
        await using var handle1 = await lock1.TryAcquireAsync();
        Assert.That(handle1, Is.Not.Null);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await this._etcdClusterSetup.TearDownCluster();
    }
}
