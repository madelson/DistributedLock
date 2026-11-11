namespace Medallion.Threading.Chaining;

public class ChainedDistributedSemaphoreProvider : IDistributedSemaphoreProvider
{
    public ChainedDistributedSemaphoreProvider(IDistributedSemaphoreProvider firstProvider, IDistributedSemaphoreProvider secondProvider, params IDistributedSemaphoreProvider[] additionalProviders) : this(lockProviders: [firstProvider, secondProvider, .. additionalProviders])
    {
    }
    internal ChainedDistributedSemaphoreProvider(IDistributedSemaphoreProvider[] lockProviders)
    {
        this.Providers = lockProviders;
    }

    internal IDistributedSemaphoreProvider[] Providers { get; }

    public IDistributedSemaphore CreateSemaphore(string name, int maxCount) => new ChainedDistributedSemaphore(name, maxCount, this.Providers.Select(p => p.CreateSemaphore(name, maxCount)).ToArray());
}
