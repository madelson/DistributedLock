#### [DistributedLock.Redis](README.md 'README')
### [Medallion.Threading.Redis](Medallion.Threading.Redis.md 'Medallion.Threading.Redis')

## RedisDistributedSemaphoreHandle Class

Implements [IDistributedSynchronizationHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle') for a [RedisDistributedSemaphore](RedisDistributedSemaphore.md 'Medallion.Threading.Redis.RedisDistributedSemaphore')

```csharp
public sealed class RedisDistributedSemaphoreHandle :
Medallion.Threading.IDistributedSynchronizationHandle,
System.IDisposable,
System.IAsyncDisposable
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; RedisDistributedSemaphoreHandle

Implements [IDistributedSynchronizationHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle'), [System.IDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IDisposable 'System.IDisposable'), [System.IAsyncDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IAsyncDisposable 'System.IAsyncDisposable')

| Properties | |
| :--- | :--- |
| [HandleLostToken](RedisDistributedSemaphoreHandle.HandleLostToken.md 'Medallion.Threading.Redis.RedisDistributedSemaphoreHandle.HandleLostToken') | Implements [HandleLostToken](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.HandleLostToken.md 'Medallion.Threading.IDistributedSynchronizationHandle.HandleLostToken') |

| Methods | |
| :--- | :--- |
| [Dispose()](RedisDistributedSemaphoreHandle.Dispose().md 'Medallion.Threading.Redis.RedisDistributedSemaphoreHandle.Dispose()') | Releases the lock |
| [DisposeAsync()](RedisDistributedSemaphoreHandle.DisposeAsync().md 'Medallion.Threading.Redis.RedisDistributedSemaphoreHandle.DisposeAsync()') | Releases the lock asynchronously |
