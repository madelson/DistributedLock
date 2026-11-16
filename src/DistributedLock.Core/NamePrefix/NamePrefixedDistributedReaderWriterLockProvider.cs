namespace Medallion.Threading.NamePrefix;

internal sealed class NamePrefixedDistributedReaderWriterLockProvider(IDistributedReaderWriterLockProvider provider, string namePrefix) : IDistributedReaderWriterLockProvider
{
    public IDistributedReaderWriterLockProvider Inner { get; } = provider;
    public string NamePrefix { get; } = namePrefix;
    public IDistributedReaderWriterLock CreateReaderWriterLock(string name) => this.Inner.CreateReaderWriterLock(this.NamePrefix + name);
}
