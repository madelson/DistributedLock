#### [DistributedLock.Redis](README.md 'README')
### [Medallion.Threading.Redis](Medallion.Threading.Redis.md 'Medallion.Threading.Redis')

## RedisDistributedLockHandle Class

Implements [IDistributedSynchronizationHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle') for [RedisDistributedLock](RedisDistributedLock.md 'Medallion.Threading.Redis.RedisDistributedLock')

```csharp
public sealed class RedisDistributedLockHandle :
Medallion.Threading.IDistributedSynchronizationHandle,
System.IDisposable,
System.IAsyncDisposable
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; RedisDistributedLockHandle

Implements [IDistributedSynchronizationHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle'), [System.IDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IDisposable 'System.IDisposable'), [System.IAsyncDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IAsyncDisposable 'System.IAsyncDisposable')

| Properties | |
| :--- | :--- |
| [HandleLostToken](RedisDistributedLockHandle.HandleLostToken.md 'Medallion.Threading.Redis.RedisDistributedLockHandle.HandleLostToken') | Implements [HandleLostToken](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.HandleLostToken.md 'Medallion.Threading.IDistributedSynchronizationHandle.HandleLostToken') |

| Methods | |
| :--- | :--- |
| [Dispose()](RedisDistributedLockHandle.Dispose().md 'Medallion.Threading.Redis.RedisDistributedLockHandle.Dispose()') | Releases the lock |
| [DisposeAsync()](RedisDistributedLockHandle.DisposeAsync().md 'Medallion.Threading.Redis.RedisDistributedLockHandle.DisposeAsync()') | Releases the lock asynchronously |
