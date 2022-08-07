#### [DistributedLock.Redis](README.md 'README')
### [Medallion.Threading.Redis](Medallion.Threading.Redis.md 'Medallion.Threading.Redis')

## RedisDistributedReaderWriterLockHandle Class

Implements [IDistributedSynchronizationHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle') for [RedisDistributedReaderWriterLock](RedisDistributedReaderWriterLock.md 'Medallion.Threading.Redis.RedisDistributedReaderWriterLock')

```csharp
public sealed class RedisDistributedReaderWriterLockHandle :
Medallion.Threading.IDistributedSynchronizationHandle,
System.IDisposable,
System.IAsyncDisposable
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; RedisDistributedReaderWriterLockHandle

Implements [IDistributedSynchronizationHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle'), [System.IDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IDisposable 'System.IDisposable'), [System.IAsyncDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IAsyncDisposable 'System.IAsyncDisposable')

| Properties | |
| :--- | :--- |
| [HandleLostToken](RedisDistributedReaderWriterLockHandle.HandleLostToken.md 'Medallion.Threading.Redis.RedisDistributedReaderWriterLockHandle.HandleLostToken') | Implements [HandleLostToken](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.HandleLostToken.md 'Medallion.Threading.IDistributedSynchronizationHandle.HandleLostToken') |

| Methods | |
| :--- | :--- |
| [Dispose()](RedisDistributedReaderWriterLockHandle.Dispose().md 'Medallion.Threading.Redis.RedisDistributedReaderWriterLockHandle.Dispose()') | Releases the lock |
| [DisposeAsync()](RedisDistributedReaderWriterLockHandle.DisposeAsync().md 'Medallion.Threading.Redis.RedisDistributedReaderWriterLockHandle.DisposeAsync()') | Releases the lock asynchronously |
