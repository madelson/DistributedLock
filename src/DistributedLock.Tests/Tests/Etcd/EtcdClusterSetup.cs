using dotnet_etcd;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Etcd;

internal class EtcdClusterSetup
{
    private readonly List<IContainer> _etcdContainers = [];
    private INetwork _network;
    private const string DockerImageName = "gcr.io/etcd-development/etcd:v3.6.1";
    private const int BaseClientPort = 2379;
    private const int BasePeerPort = 2380;
    private readonly List<string> _connectionStrings = [];


    public async Task ClusterSetup()
    {
        // setup 3 nodes etcd cluster for proper testing
        var network = new NetworkBuilder()
            .WithName("test-etcd-network")
            .Build();
        this._network = network;

        var initialCluster = string.Join(",",
            Enumerable.Range(0, 3).Select(j => $"infra{j}=http://infra{j}:{BasePeerPort + j * 2}"));
        await network.CreateAsync().ConfigureAwait(false);
        for (var i = 0; i < 3; i++)
        {
            var nodeName = $"infra{i}";
            var clientPort = BaseClientPort + i * 2;
            var peerPort = BasePeerPort + i * 2;
            var container = new ContainerBuilder()
                .WithImage(DockerImageName)
                .WithName(nodeName)
                .WithNetwork(network)
                .WithNetworkAliases(nodeName)
                .WithPortBinding(clientPort, BaseClientPort)
                .WithPortBinding(peerPort, BasePeerPort)
                .WithEnvironment("ETCD_NAME", nodeName)
                .WithEnvironment("ETCD_INITIAL_CLUSTER", initialCluster)
                .WithEnvironment("ETCD_INITIAL_CLUSTER_STATE", "new")
                .WithEnvironment("ETCD_INITIAL_CLUSTER_TOKEN", "etcd-cluster-1")
                .WithEnvironment("ETCD_INITIAL_ADVERTISE_PEER_URLS", $"http://{nodeName}:{peerPort}")
                .WithEnvironment("ETCD_LISTEN_PEER_URLS", $"http://0.0.0.0:{peerPort}")
                .WithEnvironment("ETCD_LISTEN_CLIENT_URLS", $"http://0.0.0.0:{clientPort}")
                .WithEnvironment("ETCD_ADVERTISE_CLIENT_URLS", $"http://localhost:{clientPort}")
                .Build();
            this._etcdContainers.Add(container);
            this._connectionStrings.Add($"http://localhost:{clientPort}");
        }


        TestContext.WriteLine("This is a message to the test output.");
        var tasks = this._etcdContainers.Select(container => container.StartAsync());
        TestContext.WriteLine("This is a message to the test output.");
        await Task.WhenAll(tasks);
    }

    public async Task TearDownCluster()
    {
        var valueTasks = this._etcdContainers.Select(container => container.DisposeAsync());
        foreach (var task in valueTasks)
        {
            await task;
        }

        await this._network.DisposeAsync();
    }

    internal EtcdClient CreateClientToEtcdCluster()
    {
        return new EtcdClient(string.Join(",", this._connectionStrings));
    }
}


[SetUpFixture]
public class EtcdSetupFixture
{
    internal static readonly EtcdClusterSetup EtcdClusterSetup = new EtcdClusterSetup();

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await EtcdClusterSetup.ClusterSetup();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await EtcdClusterSetup.TearDownCluster();
    }
}
