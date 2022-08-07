#### [DistributedLock.Oracle](README.md 'README')
### [Medallion.Threading.Oracle](Medallion.Threading.Oracle.md 'Medallion.Threading.Oracle')

## OracleDistributedReaderWriterLockUpgradeableHandle Class

Implements [IDistributedLockUpgradeableHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLockUpgradeableHandle.md 'Medallion.Threading.IDistributedLockUpgradeableHandle')

```csharp
public sealed class OracleDistributedReaderWriterLockUpgradeableHandle : Medallion.Threading.Oracle.OracleDistributedReaderWriterLockHandle,
Medallion.Threading.IDistributedLockUpgradeableHandle,
Medallion.Threading.IDistributedSynchronizationHandle,
System.IDisposable,
System.IAsyncDisposable
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; [OracleDistributedReaderWriterLockHandle](OracleDistributedReaderWriterLockHandle.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLockHandle') &#129106; OracleDistributedReaderWriterLockUpgradeableHandle

Implements [IDistributedLockUpgradeableHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLockUpgradeableHandle.md 'Medallion.Threading.IDistributedLockUpgradeableHandle'), [IDistributedSynchronizationHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle'), [System.IDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IDisposable 'System.IDisposable'), [System.IAsyncDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IAsyncDisposable 'System.IAsyncDisposable')

| Properties | |
| :--- | :--- |
| [HandleLostToken](OracleDistributedReaderWriterLockUpgradeableHandle.HandleLostToken.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLockUpgradeableHandle.HandleLostToken') | Implements [HandleLostToken](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.HandleLostToken.md 'Medallion.Threading.IDistributedSynchronizationHandle.HandleLostToken') |

| Methods | |
| :--- | :--- |
| [DisposeAsync()](OracleDistributedReaderWriterLockUpgradeableHandle.DisposeAsync().md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLockUpgradeableHandle.DisposeAsync()') | Releases the lock asynchronously |
| [TryUpgradeToWriteLock(TimeSpan, CancellationToken)](OracleDistributedReaderWriterLockUpgradeableHandle.TryUpgradeToWriteLock./jHol+SIt7DlqMKC8v111g.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLockUpgradeableHandle.TryUpgradeToWriteLock(System.TimeSpan, System.Threading.CancellationToken)') | Implements [TryUpgradeToWriteLock(TimeSpan, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLockUpgradeableHandle.TryUpgradeToWriteLock.kj/tM1emIKPQTMhyxJaEVg.md 'Medallion.Threading.IDistributedLockUpgradeableHandle.TryUpgradeToWriteLock(System.TimeSpan,System.Threading.CancellationToken)') |
| [TryUpgradeToWriteLockAsync(TimeSpan, CancellationToken)](OracleDistributedReaderWriterLockUpgradeableHandle.TryUpgradeToWriteLockAsync.FgoBDmLlHiktYNvHXL6NPw.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLockUpgradeableHandle.TryUpgradeToWriteLockAsync(System.TimeSpan, System.Threading.CancellationToken)') | Implements [TryUpgradeToWriteLockAsync(TimeSpan, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLockUpgradeableHandle.TryUpgradeToWriteLockAsync.wN/ZfUBdRwlMtX9ctB9dtg.md 'Medallion.Threading.IDistributedLockUpgradeableHandle.TryUpgradeToWriteLockAsync(System.TimeSpan,System.Threading.CancellationToken)') |
| [UpgradeToWriteLock(Nullable&lt;TimeSpan&gt;, CancellationToken)](OracleDistributedReaderWriterLockUpgradeableHandle.UpgradeToWriteLock.hWqwyiLC1304nrfXA65g6A.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLockUpgradeableHandle.UpgradeToWriteLock(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Implements [UpgradeToWriteLock(Nullable&lt;TimeSpan&gt;, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLockUpgradeableHandle.UpgradeToWriteLock.Tz7MsKLra+HymbjqGwuzRQ.md 'Medallion.Threading.IDistributedLockUpgradeableHandle.UpgradeToWriteLock(System.Nullable{System.TimeSpan},System.Threading.CancellationToken)') |
| [UpgradeToWriteLockAsync(Nullable&lt;TimeSpan&gt;, CancellationToken)](OracleDistributedReaderWriterLockUpgradeableHandle.UpgradeToWriteLockAsync.NiMBlMvyOdtLajlwUeLO1Q.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLockUpgradeableHandle.UpgradeToWriteLockAsync(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Implements [UpgradeToWriteLockAsync(Nullable&lt;TimeSpan&gt;, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLockUpgradeableHandle.UpgradeToWriteLockAsync.5X2Faf/VtyA3b0uF1JN1kg.md 'Medallion.Threading.IDistributedLockUpgradeableHandle.UpgradeToWriteLockAsync(System.Nullable{System.TimeSpan},System.Threading.CancellationToken)') |
