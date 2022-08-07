#### [DistributedLock.Oracle](README.md 'README')
### [Medallion.Threading.Oracle](Medallion.Threading.Oracle.md 'Medallion.Threading.Oracle').[OracleDistributedReaderWriterLock](OracleDistributedReaderWriterLock.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLock')

## OracleDistributedReaderWriterLock.TryAcquireUpgradeableReadLock(TimeSpan, CancellationToken) Method

Attempts to acquire an UPGRADE lock synchronously. Not compatible with another UPGRADE lock or a WRITE lock. Usage:   
  
```csharp  
using (var handle = myLock.TryAcquireUpgradeableReadLock(...))  
{  
    if (handle != null) { /* we have the lock! */ }  
}  
// dispose releases the lock if we took it  
```

```csharp
public Medallion.Threading.Oracle.OracleDistributedReaderWriterLockUpgradeableHandle? TryAcquireUpgradeableReadLock(System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.Oracle.OracleDistributedReaderWriterLock.TryAcquireUpgradeableReadLock(System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

How long to wait before giving up on the acquisition attempt. Defaults to 0

<a name='Medallion.Threading.Oracle.OracleDistributedReaderWriterLock.TryAcquireUpgradeableReadLock(System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [TryAcquireUpgradeableReadLock(TimeSpan, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedUpgradeableReaderWriterLock.TryAcquireUpgradeableReadLock.NcomTiK4v4VsrD5p8zrY6A.md 'Medallion.Threading.IDistributedUpgradeableReaderWriterLock.TryAcquireUpgradeableReadLock(System.TimeSpan,System.Threading.CancellationToken)')

#### Returns
[OracleDistributedReaderWriterLockUpgradeableHandle](OracleDistributedReaderWriterLockUpgradeableHandle.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLockUpgradeableHandle')  
An [OracleDistributedReaderWriterLockUpgradeableHandle](OracleDistributedReaderWriterLockUpgradeableHandle.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLockUpgradeableHandle') which can be used to release the lock or null on failure