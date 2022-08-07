#### [DistributedLock.SqlServer](README.md 'README')
### [Medallion.Threading.SqlServer](Medallion.Threading.SqlServer.md 'Medallion.Threading.SqlServer')

## SqlDistributedReaderWriterLockUpgradeableHandle Class

Implements [IDistributedLockUpgradeableHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLockUpgradeableHandle.md 'Medallion.Threading.IDistributedLockUpgradeableHandle')

```csharp
public sealed class SqlDistributedReaderWriterLockUpgradeableHandle : Medallion.Threading.SqlServer.SqlDistributedReaderWriterLockHandle,
Medallion.Threading.IDistributedLockUpgradeableHandle,
Medallion.Threading.IDistributedSynchronizationHandle,
System.IDisposable,
System.IAsyncDisposable
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; [SqlDistributedReaderWriterLockHandle](SqlDistributedReaderWriterLockHandle.md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLockHandle') &#129106; SqlDistributedReaderWriterLockUpgradeableHandle

Implements [IDistributedLockUpgradeableHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLockUpgradeableHandle.md 'Medallion.Threading.IDistributedLockUpgradeableHandle'), [IDistributedSynchronizationHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle'), [System.IDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IDisposable 'System.IDisposable'), [System.IAsyncDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IAsyncDisposable 'System.IAsyncDisposable')

| Properties | |
| :--- | :--- |
| [HandleLostToken](SqlDistributedReaderWriterLockUpgradeableHandle.HandleLostToken.md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLockUpgradeableHandle.HandleLostToken') | Implements [HandleLostToken](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.HandleLostToken.md 'Medallion.Threading.IDistributedSynchronizationHandle.HandleLostToken') |

| Methods | |
| :--- | :--- |
| [DisposeAsync()](SqlDistributedReaderWriterLockUpgradeableHandle.DisposeAsync().md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLockUpgradeableHandle.DisposeAsync()') | Releases the lock asynchronously |
| [TryUpgradeToWriteLock(TimeSpan, CancellationToken)](SqlDistributedReaderWriterLockUpgradeableHandle.TryUpgradeToWriteLock.zMUOGQVaJkWs3VcJXp+UCw.md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLockUpgradeableHandle.TryUpgradeToWriteLock(System.TimeSpan, System.Threading.CancellationToken)') | Implements [TryUpgradeToWriteLock(TimeSpan, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLockUpgradeableHandle.TryUpgradeToWriteLock.kj/tM1emIKPQTMhyxJaEVg.md 'Medallion.Threading.IDistributedLockUpgradeableHandle.TryUpgradeToWriteLock(System.TimeSpan,System.Threading.CancellationToken)') |
| [TryUpgradeToWriteLockAsync(TimeSpan, CancellationToken)](SqlDistributedReaderWriterLockUpgradeableHandle.TryUpgradeToWriteLockAsync.8oq6BCJTyac4Rk/328XUBw.md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLockUpgradeableHandle.TryUpgradeToWriteLockAsync(System.TimeSpan, System.Threading.CancellationToken)') | Implements [TryUpgradeToWriteLockAsync(TimeSpan, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLockUpgradeableHandle.TryUpgradeToWriteLockAsync.wN/ZfUBdRwlMtX9ctB9dtg.md 'Medallion.Threading.IDistributedLockUpgradeableHandle.TryUpgradeToWriteLockAsync(System.TimeSpan,System.Threading.CancellationToken)') |
| [UpgradeToWriteLock(Nullable&lt;TimeSpan&gt;, CancellationToken)](SqlDistributedReaderWriterLockUpgradeableHandle.UpgradeToWriteLock.jbRB6OR/9hXGBo4CJ+m0bw.md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLockUpgradeableHandle.UpgradeToWriteLock(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Implements [UpgradeToWriteLock(Nullable&lt;TimeSpan&gt;, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLockUpgradeableHandle.UpgradeToWriteLock.Tz7MsKLra+HymbjqGwuzRQ.md 'Medallion.Threading.IDistributedLockUpgradeableHandle.UpgradeToWriteLock(System.Nullable{System.TimeSpan},System.Threading.CancellationToken)') |
| [UpgradeToWriteLockAsync(Nullable&lt;TimeSpan&gt;, CancellationToken)](SqlDistributedReaderWriterLockUpgradeableHandle.UpgradeToWriteLockAsync.Vi+8+okgqbatuvWRASkevg.md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLockUpgradeableHandle.UpgradeToWriteLockAsync(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Implements [UpgradeToWriteLockAsync(Nullable&lt;TimeSpan&gt;, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLockUpgradeableHandle.UpgradeToWriteLockAsync.5X2Faf/VtyA3b0uF1JN1kg.md 'Medallion.Threading.IDistributedLockUpgradeableHandle.UpgradeToWriteLockAsync(System.Nullable{System.TimeSpan},System.Threading.CancellationToken)') |
