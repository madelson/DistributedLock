#### [DistributedLock.Redis](README.md 'README')
### [Medallion.Threading.Redis](Medallion.Threading.Redis.md 'Medallion.Threading.Redis').[RedisDistributedReaderWriterLock](RedisDistributedReaderWriterLock.md 'Medallion.Threading.Redis.RedisDistributedReaderWriterLock')

## RedisDistributedReaderWriterLock.AcquireWriteLock(Nullable<TimeSpan>, CancellationToken) Method

Acquires a WRITE lock synchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Not compatible with another WRITE lock or an UPGRADE lock. Usage: 

```csharp
using (myLock.AcquireWriteLock(...))
{
    /* we have the lock! */
}
// dispose releases the lock
```

```csharp
public Medallion.Threading.Redis.RedisDistributedReaderWriterLockHandle AcquireWriteLock(System.Nullable<System.TimeSpan> timeout=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.Redis.RedisDistributedReaderWriterLock.AcquireWriteLock(System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.Nullable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')[System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')

How long to wait before giving up on the acquisition attempt. Defaults to [System.Threading.Timeout.InfiniteTimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Timeout.InfiniteTimeSpan 'System.Threading.Timeout.InfiniteTimeSpan')

<a name='Medallion.Threading.Redis.RedisDistributedReaderWriterLock.AcquireWriteLock(System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [AcquireWriteLock(Nullable&lt;TimeSpan&gt;, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedReaderWriterLock.AcquireWriteLock.8zDirrYDrgr0WzrsnH7blA.md 'Medallion.Threading.IDistributedReaderWriterLock.AcquireWriteLock(System.Nullable{System.TimeSpan},System.Threading.CancellationToken)')

#### Returns
[RedisDistributedReaderWriterLockHandle](RedisDistributedReaderWriterLockHandle.md 'Medallion.Threading.Redis.RedisDistributedReaderWriterLockHandle')  
A [RedisDistributedReaderWriterLockHandle](RedisDistributedReaderWriterLockHandle.md 'Medallion.Threading.Redis.RedisDistributedReaderWriterLockHandle') which can be used to release the lock