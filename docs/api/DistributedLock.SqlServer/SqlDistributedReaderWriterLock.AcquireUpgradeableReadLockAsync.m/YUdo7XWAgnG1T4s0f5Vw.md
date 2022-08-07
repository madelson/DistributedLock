#### [DistributedLock.SqlServer](README.md 'README')
### [Medallion.Threading.SqlServer](Medallion.Threading.SqlServer.md 'Medallion.Threading.SqlServer').[SqlDistributedReaderWriterLock](SqlDistributedReaderWriterLock.md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock')

## SqlDistributedReaderWriterLock.AcquireUpgradeableReadLockAsync(Nullable<TimeSpan>, CancellationToken) Method

Acquires an UPGRADE lock asynchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Not compatible with another UPGRADE lock or a WRITE lock. Usage:   
  
```csharp  
await using (await myLock.AcquireUpgradeableReadLockAsync(...))  
{  
    /* we have the lock! */  
}  
// dispose releases the lock  
```

```csharp
public System.Threading.Tasks.ValueTask<Medallion.Threading.SqlServer.SqlDistributedReaderWriterLockUpgradeableHandle> AcquireUpgradeableReadLockAsync(System.Nullable<System.TimeSpan> timeout=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.AcquireUpgradeableReadLockAsync(System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.Nullable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')[System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')

How long to wait before giving up on the acquisition attempt. Defaults to [System.Threading.Timeout.InfiniteTimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Timeout.InfiniteTimeSpan 'System.Threading.Timeout.InfiniteTimeSpan')

<a name='Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.AcquireUpgradeableReadLockAsync(System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [AcquireUpgradeableReadLockAsync(Nullable&lt;TimeSpan&gt;, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedUpgradeableReaderWriterLock.AcquireUpgradeableReadLockAsync.XDD/LbfIJrScMNU14D5OvA.md 'Medallion.Threading.IDistributedUpgradeableReaderWriterLock.AcquireUpgradeableReadLockAsync(System.Nullable{System.TimeSpan},System.Threading.CancellationToken)')

#### Returns
[System.Threading.Tasks.ValueTask&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')[SqlDistributedReaderWriterLockUpgradeableHandle](SqlDistributedReaderWriterLockUpgradeableHandle.md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLockUpgradeableHandle')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')  
A [SqlDistributedReaderWriterLockUpgradeableHandle](SqlDistributedReaderWriterLockUpgradeableHandle.md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLockUpgradeableHandle') which can be used to release the lock