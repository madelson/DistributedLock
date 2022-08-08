#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading').[IDistributedReaderWriterLock](IDistributedReaderWriterLock.md 'Medallion.Threading.IDistributedReaderWriterLock')

## IDistributedReaderWriterLock.AcquireWriteLock(Nullable<TimeSpan>, CancellationToken) Method

Acquires a WRITE lock synchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Not compatible with another WRITE lock or an UPGRADE lock. Usage: 

```csharp
using (myLock.AcquireWriteLock(...))
{
    /* we have the lock! */
}
// dispose releases the lock
```

```csharp
Medallion.Threading.IDistributedSynchronizationHandle AcquireWriteLock(System.Nullable<System.TimeSpan> timeout=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.IDistributedReaderWriterLock.AcquireWriteLock(System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.Nullable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')[System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')

How long to wait before giving up on the acquisition attempt. Defaults to [System.Threading.Timeout.InfiniteTimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Timeout.InfiniteTimeSpan 'System.Threading.Timeout.InfiniteTimeSpan')

<a name='Medallion.Threading.IDistributedReaderWriterLock.AcquireWriteLock(System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

#### Returns
[IDistributedSynchronizationHandle](IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle')  
An [IDistributedSynchronizationHandle](IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle') which can be used to release the lock