namespace Medallion.Threading.Chaining;
public class ChainedDistributedLockProvider : IDistributedLockProvider
{
    public ChainedDistributedLockProvider(IDistributedLockProvider firstProvider, IDistributedLockProvider secondProvider, params IDistributedLockProvider[] additionalProviders) : this(lockProviders: [firstProvider, secondProvider, .. additionalProviders])
    {
    }
    internal ChainedDistributedLockProvider(IDistributedLockProvider[] lockProviders)
    {
        this.Providers = lockProviders;
    }

    public IDistributedLockProvider[] Providers { get; }

    public IDistributedLock CreateLock(string name) => new ChainedDistributedLock(name, this.Providers.Select(p => p.CreateLock(name)).ToArray());
}