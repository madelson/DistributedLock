#### [DistributedLock.SqlServer](README.md 'README')
### [Medallion.Threading.SqlServer](Medallion.Threading.SqlServer.md 'Medallion.Threading.SqlServer').[SqlDistributedReaderWriterLock](SqlDistributedReaderWriterLock.md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock')

## SqlDistributedReaderWriterLock.TryAcquireUpgradeableReadLockAsync(TimeSpan, CancellationToken) Method

Attempts to acquire an UPGRADE lock asynchronously. Not compatible with another UPGRADE lock or a WRITE lock. Usage: 

```csharp
await using (var handle = await myLock.TryAcquireUpgradeableReadLockAsync(...))
{
    if (handle != null) { /* we have the lock! */ }
}
// dispose releases the lock if we took it
```

```csharp
public System.Threading.Tasks.ValueTask<Medallion.Threading.SqlServer.SqlDistributedReaderWriterLockUpgradeableHandle?> TryAcquireUpgradeableReadLockAsync(System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.TryAcquireUpgradeableReadLockAsync(System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

How long to wait before giving up on the acquisition attempt. Defaults to 0

<a name='Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.TryAcquireUpgradeableReadLockAsync(System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [TryAcquireUpgradeableReadLockAsync(TimeSpan, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedUpgradeableReaderWriterLock.TryAcquireUpgradeableReadLockAsync.NeQQ4jMkCO0IteXQSJv/1w.md 'Medallion.Threading.IDistributedUpgradeableReaderWriterLock.TryAcquireUpgradeableReadLockAsync(System.TimeSpan,System.Threading.CancellationToken)')

#### Returns
[System.Threading.Tasks.ValueTask&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')[SqlDistributedReaderWriterLockUpgradeableHandle](SqlDistributedReaderWriterLockUpgradeableHandle.md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLockUpgradeableHandle')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')  
A [SqlDistributedReaderWriterLockUpgradeableHandle](SqlDistributedReaderWriterLockUpgradeableHandle.md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLockUpgradeableHandle') which can be used to release the lock or null on failure