namespace Medallion.Threading.Chaining;

public class ChainedDistributedReaderWriterLockProvider : IDistributedReaderWriterLockProvider
{
    public ChainedDistributedReaderWriterLockProvider(IDistributedReaderWriterLockProvider firstProvider, IDistributedReaderWriterLockProvider secondProvider, params IDistributedReaderWriterLockProvider[] additionalProviders) : this(lockProviders: [firstProvider, secondProvider, .. additionalProviders])
    {
    }
    internal ChainedDistributedReaderWriterLockProvider(IDistributedReaderWriterLockProvider[] lockProviders)
    {
        this.Providers = lockProviders;
    }

    public IDistributedReaderWriterLockProvider[] Providers { get; }
    public IDistributedReaderWriterLock CreateReaderWriterLock(string name) => new ChainedDistributedReaderWriterLock(name, this.Providers.Select(p => p.CreateReaderWriterLock(name)).ToArray());
}
