using Azure.Storage.Blobs;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using NUnit.Framework;
using System.Diagnostics;

namespace Medallion.Threading.Tests.Azure;

[SetUpFixture]
public class AzureSetUpFixture
{
    private const string DockerImageName = "mcr.microsoft.com/azure-storage/azurite";
    private const int BlobServicePort = 10000;
    private IContainer? _container;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        // Create a new instance of a container.
        this._container = new ContainerBuilder()
            .WithImage(DockerImageName)
            // Bind port 8080 of the container to a random port on the host.
            .WithPortBinding(BlobServicePort, 10000)
            // Wait until the HTTP endpoint of the container is available.
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(10000))
            // Build the container configuration.
            .Build();
        await this._container.StartAsync()
            .ConfigureAwait(false);
        // Wait for Azurite to be ready

        await new BlobContainerClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName)
            .CreateIfNotExistsAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await new BlobContainerClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName)
            .DeleteIfExistsAsync();
        await this._container.DisposeAsync();
    }
}