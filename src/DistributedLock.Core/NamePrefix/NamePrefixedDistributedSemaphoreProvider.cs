namespace Medallion.Threading.NamePrefix;

internal sealed class NamePrefixedDistributedSemaphoreProvider(IDistributedSemaphoreProvider provider, string namePrefix) : IDistributedSemaphoreProvider
{
    public IDistributedSemaphoreProvider Inner { get; } = provider;
    public string NamePrefix { get; } = namePrefix;
    public IDistributedSemaphore CreateSemaphore(string name, int maxCount) => this.Inner.CreateSemaphore(this.NamePrefix + name, maxCount);
}
