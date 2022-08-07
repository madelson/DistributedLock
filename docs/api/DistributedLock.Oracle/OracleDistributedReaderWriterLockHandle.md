#### [DistributedLock.Oracle](README.md 'README')
### [Medallion.Threading.Oracle](Medallion.Threading.Oracle.md 'Medallion.Threading.Oracle')

## OracleDistributedReaderWriterLockHandle Class

Implements [IDistributedSynchronizationHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle')

```csharp
public abstract class OracleDistributedReaderWriterLockHandle :
Medallion.Threading.IDistributedSynchronizationHandle,
System.IDisposable,
System.IAsyncDisposable
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; OracleDistributedReaderWriterLockHandle

Derived  
&#8627; [OracleDistributedReaderWriterLockUpgradeableHandle](OracleDistributedReaderWriterLockUpgradeableHandle.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLockUpgradeableHandle')

Implements [IDistributedSynchronizationHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle'), [System.IDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IDisposable 'System.IDisposable'), [System.IAsyncDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IAsyncDisposable 'System.IAsyncDisposable')

| Properties | |
| :--- | :--- |
| [HandleLostToken](OracleDistributedReaderWriterLockHandle.HandleLostToken.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLockHandle.HandleLostToken') | Implements [HandleLostToken](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.HandleLostToken.md 'Medallion.Threading.IDistributedSynchronizationHandle.HandleLostToken') |

| Methods | |
| :--- | :--- |
| [Dispose()](OracleDistributedReaderWriterLockHandle.Dispose().md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLockHandle.Dispose()') | Releases the lock |
| [DisposeAsync()](OracleDistributedReaderWriterLockHandle.DisposeAsync().md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLockHandle.DisposeAsync()') | Releases the lock asynchronously |
