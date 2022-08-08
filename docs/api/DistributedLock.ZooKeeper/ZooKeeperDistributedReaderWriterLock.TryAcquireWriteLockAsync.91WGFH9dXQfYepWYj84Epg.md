#### [DistributedLock.ZooKeeper](README.md 'README')
### [Medallion.Threading.ZooKeeper](Medallion.Threading.ZooKeeper.md 'Medallion.Threading.ZooKeeper').[ZooKeeperDistributedReaderWriterLock](ZooKeeperDistributedReaderWriterLock.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock')

## ZooKeeperDistributedReaderWriterLock.TryAcquireWriteLockAsync(TimeSpan, CancellationToken) Method

Attempts to acquire a WRITE lock asynchronously. Not compatible with another WRITE lock or an UPGRADE lock. Usage: 

```csharp
await using (var handle = await myLock.TryAcquireWriteLockAsync(...))
{
    if (handle != null) { /* we have the lock! */ }
}
// dispose releases the lock if we took it
```

```csharp
public System.Threading.Tasks.ValueTask<Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLockHandle?> TryAcquireWriteLockAsync(System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.TryAcquireWriteLockAsync(System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

How long to wait before giving up on the acquisition attempt. Defaults to 0

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLock.TryAcquireWriteLockAsync(System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [TryAcquireWriteLockAsync(TimeSpan, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedReaderWriterLock.TryAcquireWriteLockAsync.yhTsitSwERpacPdxWmUvww.md 'Medallion.Threading.IDistributedReaderWriterLock.TryAcquireWriteLockAsync(System.TimeSpan,System.Threading.CancellationToken)')

#### Returns
[System.Threading.Tasks.ValueTask&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')[ZooKeeperDistributedReaderWriterLockHandle](ZooKeeperDistributedReaderWriterLockHandle.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLockHandle')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')  
A [ZooKeeperDistributedReaderWriterLockHandle](ZooKeeperDistributedReaderWriterLockHandle.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedReaderWriterLockHandle') which can be used to release the lock or null on failure