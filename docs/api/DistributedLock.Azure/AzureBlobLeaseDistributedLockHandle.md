#### [DistributedLock.Azure](README.md 'README')
### [Medallion.Threading.Azure](Medallion.Threading.Azure.md 'Medallion.Threading.Azure')

## AzureBlobLeaseDistributedLockHandle Class

Implements [IDistributedSynchronizationHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle')

```csharp
public sealed class AzureBlobLeaseDistributedLockHandle :
Medallion.Threading.IDistributedSynchronizationHandle,
System.IDisposable,
System.IAsyncDisposable
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; AzureBlobLeaseDistributedLockHandle

Implements [IDistributedSynchronizationHandle](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle'), [System.IDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IDisposable 'System.IDisposable'), [System.IAsyncDisposable](https://docs.microsoft.com/en-us/dotnet/api/System.IAsyncDisposable 'System.IAsyncDisposable')

| Properties | |
| :--- | :--- |
| [HandleLostToken](AzureBlobLeaseDistributedLockHandle.HandleLostToken.md 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLockHandle.HandleLostToken') | Implements [HandleLostToken](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSynchronizationHandle.HandleLostToken.md 'Medallion.Threading.IDistributedSynchronizationHandle.HandleLostToken') |
| [LeaseId](AzureBlobLeaseDistributedLockHandle.LeaseId.md 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLockHandle.LeaseId') | The underlying Azure lease ID |

| Methods | |
| :--- | :--- |
| [Dispose()](AzureBlobLeaseDistributedLockHandle.Dispose().md 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLockHandle.Dispose()') | Releases the lock |
| [DisposeAsync()](AzureBlobLeaseDistributedLockHandle.DisposeAsync().md 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLockHandle.DisposeAsync()') | Releases the lock asynchronously |
