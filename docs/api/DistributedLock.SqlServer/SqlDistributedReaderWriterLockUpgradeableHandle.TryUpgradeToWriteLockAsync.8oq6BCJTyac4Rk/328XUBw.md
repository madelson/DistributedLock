#### [DistributedLock.SqlServer](README.md 'README')
### [Medallion.Threading.SqlServer](Medallion.Threading.SqlServer.md 'Medallion.Threading.SqlServer').[SqlDistributedReaderWriterLockUpgradeableHandle](SqlDistributedReaderWriterLockUpgradeableHandle.md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLockUpgradeableHandle')

## SqlDistributedReaderWriterLockUpgradeableHandle.TryUpgradeToWriteLockAsync(TimeSpan, CancellationToken) Method

Implements [TryUpgradeToWriteLockAsync(TimeSpan, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLockUpgradeableHandle.TryUpgradeToWriteLockAsync.wN/ZfUBdRwlMtX9ctB9dtg.md 'Medallion.Threading.IDistributedLockUpgradeableHandle.TryUpgradeToWriteLockAsync(System.TimeSpan,System.Threading.CancellationToken)')

```csharp
public System.Threading.Tasks.ValueTask<bool> TryUpgradeToWriteLockAsync(System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.SqlServer.SqlDistributedReaderWriterLockUpgradeableHandle.TryUpgradeToWriteLockAsync(System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

<a name='Medallion.Threading.SqlServer.SqlDistributedReaderWriterLockUpgradeableHandle.TryUpgradeToWriteLockAsync(System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Implements [TryUpgradeToWriteLockAsync(TimeSpan, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLockUpgradeableHandle.TryUpgradeToWriteLockAsync.wN/ZfUBdRwlMtX9ctB9dtg.md 'Medallion.Threading.IDistributedLockUpgradeableHandle.TryUpgradeToWriteLockAsync(System.TimeSpan,System.Threading.CancellationToken)')

#### Returns
[System.Threading.Tasks.ValueTask&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')[System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')