#### [DistributedLock.Azure](README.md 'README')
### [Medallion.Threading.Azure](Medallion.Threading.Azure.md 'Medallion.Threading.Azure').[AzureBlobLeaseDistributedLock](AzureBlobLeaseDistributedLock.md 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLock')

## AzureBlobLeaseDistributedLock.Acquire(Nullable<TimeSpan>, CancellationToken) Method

Acquires the lock synchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Usage: 

```csharp
using (myLock.Acquire(...))
{
    /* we have the lock! */
}
// dispose releases the lock
```

```csharp
public Medallion.Threading.Azure.AzureBlobLeaseDistributedLockHandle Acquire(System.Nullable<System.TimeSpan> timeout=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.Acquire(System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.Nullable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')[System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')

How long to wait before giving up on the acquisition attempt. Defaults to [System.Threading.Timeout.InfiniteTimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Timeout.InfiniteTimeSpan 'System.Threading.Timeout.InfiniteTimeSpan')

<a name='Medallion.Threading.Azure.AzureBlobLeaseDistributedLock.Acquire(System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [Acquire(Nullable&lt;TimeSpan&gt;, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLock.Acquire.Q+8FXimBZqUrDv5tTRw59w.md 'Medallion.Threading.IDistributedLock.Acquire(System.Nullable{System.TimeSpan},System.Threading.CancellationToken)')

#### Returns
[AzureBlobLeaseDistributedLockHandle](AzureBlobLeaseDistributedLockHandle.md 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLockHandle')  
An [AzureBlobLeaseDistributedLockHandle](AzureBlobLeaseDistributedLockHandle.md 'Medallion.Threading.Azure.AzureBlobLeaseDistributedLockHandle') which can be used to release the lock