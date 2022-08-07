#### [DistributedLock.SqlServer](README.md 'README')
### [Medallion.Threading.SqlServer](Medallion.Threading.SqlServer.md 'Medallion.Threading.SqlServer')

## SqlDistributedReaderWriterLockHandle Class

Implements [IDistributedSynchronizationHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle')

```csharp
public abstract class SqlDistributedReaderWriterLockHandle :
Medallion.Threading.IDistributedSynchronizationHandle,
System.IDisposable,
System.IAsyncDisposable
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; SqlDistributedReaderWriterLockHandle

Derived  
&#8627; [SqlDistributedReaderWriterLockUpgradeableHandle](SqlDistributedReaderWriterLockUpgradeableHandle.md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLockUpgradeableHandle')

Implements [IDistributedSynchronizationHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle'), [System.IDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IDisposable 'System.IDisposable'), [System.IAsyncDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IAsyncDisposable 'System.IAsyncDisposable')

| Properties | |
| :--- | :--- |
| [HandleLostToken](SqlDistributedReaderWriterLockHandle.HandleLostToken.md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLockHandle.HandleLostToken') | Implements [HandleLostToken](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.HandleLostToken.md 'Medallion.Threading.IDistributedSynchronizationHandle.HandleLostToken') |

| Methods | |
| :--- | :--- |
| [Dispose()](SqlDistributedReaderWriterLockHandle.Dispose().md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLockHandle.Dispose()') | Releases the lock |
| [DisposeAsync()](SqlDistributedReaderWriterLockHandle.DisposeAsync().md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLockHandle.DisposeAsync()') | Releases the lock asynchronously |
