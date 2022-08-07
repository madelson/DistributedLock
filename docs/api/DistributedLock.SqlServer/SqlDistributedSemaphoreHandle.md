#### [DistributedLock.SqlServer](README.md 'README')
### [Medallion.Threading.SqlServer](Medallion.Threading.SqlServer.md 'Medallion.Threading.SqlServer')

## SqlDistributedSemaphoreHandle Class

Implements [IDistributedSynchronizationHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle')

```csharp
public sealed class SqlDistributedSemaphoreHandle :
Medallion.Threading.IDistributedSynchronizationHandle,
System.IDisposable,
System.IAsyncDisposable
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; SqlDistributedSemaphoreHandle

Implements [IDistributedSynchronizationHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle'), [System.IDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IDisposable 'System.IDisposable'), [System.IAsyncDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IAsyncDisposable 'System.IAsyncDisposable')

| Properties | |
| :--- | :--- |
| [HandleLostToken](SqlDistributedSemaphoreHandle.HandleLostToken.md 'Medallion.Threading.SqlServer.SqlDistributedSemaphoreHandle.HandleLostToken') | Implements [HandleLostToken](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.HandleLostToken.md 'Medallion.Threading.IDistributedSynchronizationHandle.HandleLostToken') |

| Methods | |
| :--- | :--- |
| [Dispose()](SqlDistributedSemaphoreHandle.Dispose().md 'Medallion.Threading.SqlServer.SqlDistributedSemaphoreHandle.Dispose()') | Releases the semaphore |
| [DisposeAsync()](SqlDistributedSemaphoreHandle.DisposeAsync().md 'Medallion.Threading.SqlServer.SqlDistributedSemaphoreHandle.DisposeAsync()') | Releases the semaphore asynchronously |
