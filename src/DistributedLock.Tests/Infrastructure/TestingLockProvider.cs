namespace Medallion.Threading.Tests;

public abstract class TestingLockProvider<TStrategy> : ITestingNameProvider, IDisposable
    where TStrategy : TestingSynchronizationStrategy, new()
{
    private readonly Lazy<TStrategy> _lazyStrategy = new(() => new TStrategy());

    public virtual TStrategy Strategy => this._lazyStrategy.Value;

    public virtual bool SupportsCrossProcessAbandonment => true;

    public abstract IDistributedLock CreateLockWithExactName(string name);
    public abstract string GetSafeName(string name);

    public virtual string GetCrossProcessLockType() => this.CreateLock(string.Empty).GetType().Name;
    public virtual void Dispose() => this.Strategy.Dispose();

    public virtual string GetConnectionStringForCrossProcessTest() => "<connection-string>";

    /// <summary>
    /// Returns a lock whose name is based on <see cref="TestingNameProviderExtensions.GetUniqueSafeName(ITestingNameProvider, string)"/>
    /// </summary>
    public IDistributedLock CreateLock(string baseName) =>
        this.CreateLockWithExactName(this.GetUniqueSafeName(baseName));
}
