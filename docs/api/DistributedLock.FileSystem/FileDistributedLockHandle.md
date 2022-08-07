#### [DistributedLock.FileSystem](README.md 'README')
### [Medallion.Threading.FileSystem](Medallion.Threading.FileSystem.md 'Medallion.Threading.FileSystem')

## FileDistributedLockHandle Class

Implements [IDistributedSynchronizationHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle')

```csharp
public sealed class FileDistributedLockHandle :
Medallion.Threading.IDistributedSynchronizationHandle,
System.IDisposable,
System.IAsyncDisposable
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; FileDistributedLockHandle

Implements [IDistributedSynchronizationHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle'), [System.IDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IDisposable 'System.IDisposable'), [System.IAsyncDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IAsyncDisposable 'System.IAsyncDisposable')

| Methods | |
| :--- | :--- |
| [Dispose()](FileDistributedLockHandle.Dispose().md 'Medallion.Threading.FileSystem.FileDistributedLockHandle.Dispose()') | Releases the lock |
| [DisposeAsync()](FileDistributedLockHandle.DisposeAsync().md 'Medallion.Threading.FileSystem.FileDistributedLockHandle.DisposeAsync()') | Releases the lock |
