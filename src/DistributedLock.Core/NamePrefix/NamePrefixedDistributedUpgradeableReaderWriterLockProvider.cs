namespace Medallion.Threading.NamePrefix;

internal sealed class NamePrefixedDistributedUpgradeableReaderWriterLockProvider(IDistributedUpgradeableReaderWriterLockProvider provider, string namePrefix) : IDistributedUpgradeableReaderWriterLockProvider
{
    public IDistributedUpgradeableReaderWriterLockProvider Inner { get; } = provider;
    public string NamePrefix { get; } = namePrefix;
    public IDistributedUpgradeableReaderWriterLock CreateUpgradeableReaderWriterLock(string name) => this.Inner.CreateUpgradeableReaderWriterLock(this.NamePrefix + name);
    public IDistributedReaderWriterLock CreateReaderWriterLock(string name) => this.Inner.CreateReaderWriterLock(this.NamePrefix + name);
}
