#### [DistributedLock.SqlServer](README.md 'README')
### [Medallion.Threading.SqlServer](Medallion.Threading.SqlServer.md 'Medallion.Threading.SqlServer').[SqlDistributedReaderWriterLock](SqlDistributedReaderWriterLock.md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock')

## SqlDistributedReaderWriterLock.TryAcquireUpgradeableReadLock(TimeSpan, CancellationToken) Method

Attempts to acquire an UPGRADE lock synchronously. Not compatible with another UPGRADE lock or a WRITE lock. Usage: 

```csharp
using (var handle = myLock.TryAcquireUpgradeableReadLock(...))
{
    if (handle != null) { /* we have the lock! */ }
}
// dispose releases the lock if we took it
```

```csharp
public Medallion.Threading.SqlServer.SqlDistributedReaderWriterLockUpgradeableHandle? TryAcquireUpgradeableReadLock(System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.TryAcquireUpgradeableReadLock(System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

How long to wait before giving up on the acquisition attempt. Defaults to 0

<a name='Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.TryAcquireUpgradeableReadLock(System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [TryAcquireUpgradeableReadLock(TimeSpan, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedUpgradeableReaderWriterLock.TryAcquireUpgradeableReadLock.NcomTiK4v4VsrD5p8zrY6A.md 'Medallion.Threading.IDistributedUpgradeableReaderWriterLock.TryAcquireUpgradeableReadLock(System.TimeSpan,System.Threading.CancellationToken)')

#### Returns
[SqlDistributedReaderWriterLockUpgradeableHandle](SqlDistributedReaderWriterLockUpgradeableHandle.md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLockUpgradeableHandle')  
A [SqlDistributedReaderWriterLockUpgradeableHandle](SqlDistributedReaderWriterLockUpgradeableHandle.md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLockUpgradeableHandle') which can be used to release the lock or null on failure