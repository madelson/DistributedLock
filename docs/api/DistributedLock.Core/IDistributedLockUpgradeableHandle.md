#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading')

## IDistributedLockUpgradeableHandle Interface

A [IDistributedSynchronizationHandle](IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle') that can be upgraded to a write lock

```csharp
public interface IDistributedLockUpgradeableHandle :
Medallion.Threading.IDistributedSynchronizationHandle,
System.IDisposable,
System.IAsyncDisposable
```

Implements [IDistributedSynchronizationHandle](IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle'), [System.IDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IDisposable 'System.IDisposable'), [System.IAsyncDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IAsyncDisposable 'System.IAsyncDisposable')

| Methods | |
| :--- | :--- |
| [TryUpgradeToWriteLock(TimeSpan, CancellationToken)](IDistributedLockUpgradeableHandle.TryUpgradeToWriteLock.kj/tM1emIKPQTMhyxJaEVg.md 'Medallion.Threading.IDistributedLockUpgradeableHandle.TryUpgradeToWriteLock(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to upgrade a WRITE lock synchronously. Not compatible with another WRITE lock or a UPGRADE lock |
| [TryUpgradeToWriteLockAsync(TimeSpan, CancellationToken)](IDistributedLockUpgradeableHandle.TryUpgradeToWriteLockAsync.wN/ZfUBdRwlMtX9ctB9dtg.md 'Medallion.Threading.IDistributedLockUpgradeableHandle.TryUpgradeToWriteLockAsync(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to upgrade a WRITE lock asynchronously. Not compatible with another WRITE lock or a UPGRADE lock |
| [UpgradeToWriteLock(Nullable&lt;TimeSpan&gt;, CancellationToken)](IDistributedLockUpgradeableHandle.UpgradeToWriteLock.Tz7MsKLra+HymbjqGwuzRQ.md 'Medallion.Threading.IDistributedLockUpgradeableHandle.UpgradeToWriteLock(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Upgrades to a WRITE lock synchronously. Not compatible with another WRITE lock or a UPGRADE lock |
| [UpgradeToWriteLockAsync(Nullable&lt;TimeSpan&gt;, CancellationToken)](IDistributedLockUpgradeableHandle.UpgradeToWriteLockAsync.5X2Faf/VtyA3b0uF1JN1kg.md 'Medallion.Threading.IDistributedLockUpgradeableHandle.UpgradeToWriteLockAsync(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Upgrades to a WRITE lock asynchronously. Not compatible with another WRITE lock or a UPGRADE lock |
