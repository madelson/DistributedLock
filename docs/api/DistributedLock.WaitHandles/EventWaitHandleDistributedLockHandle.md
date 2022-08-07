#### [DistributedLock.WaitHandles](README.md 'README')
### [Medallion.Threading.WaitHandles](Medallion.Threading.WaitHandles.md 'Medallion.Threading.WaitHandles')

## EventWaitHandleDistributedLockHandle Class

Implements [IDistributedSynchronizationHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle')

```csharp
public sealed class EventWaitHandleDistributedLockHandle :
Medallion.Threading.IDistributedSynchronizationHandle,
System.IDisposable,
System.IAsyncDisposable
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; EventWaitHandleDistributedLockHandle

Implements [IDistributedSynchronizationHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle'), [System.IDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IDisposable 'System.IDisposable'), [System.IAsyncDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IAsyncDisposable 'System.IAsyncDisposable')

| Methods | |
| :--- | :--- |
| [Dispose()](EventWaitHandleDistributedLockHandle.Dispose().md 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLockHandle.Dispose()') | Releases the lock |
| [DisposeAsync()](EventWaitHandleDistributedLockHandle.DisposeAsync().md 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLockHandle.DisposeAsync()') | Releases the lock asynchronously |
