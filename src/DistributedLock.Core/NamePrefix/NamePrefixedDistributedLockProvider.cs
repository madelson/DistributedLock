namespace Medallion.Threading.NamePrefix;

internal sealed class NamePrefixedDistributedLockProvider(IDistributedLockProvider provider, string namePrefix) : IDistributedLockProvider
{
    public IDistributedLockProvider Inner { get; } = provider;
    public string NamePrefix { get; } = namePrefix;
    public IDistributedLock CreateLock(string name) => this.Inner.CreateLock(this.NamePrefix + name);
}
