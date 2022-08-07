#### [DistributedLock.Oracle](README.md 'README')
### [Medallion.Threading.Oracle](Medallion.Threading.Oracle.md 'Medallion.Threading.Oracle').[OracleDistributedReaderWriterLock](OracleDistributedReaderWriterLock.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLock')

## OracleDistributedReaderWriterLock.TryAcquireWriteLock(TimeSpan, CancellationToken) Method

Attempts to acquire a WRITE lock synchronously. Not compatible with another WRITE lock or an UPGRADE lock. Usage:   
  
```csharp  
using (var handle = myLock.TryAcquireWriteLock(...))  
{  
    if (handle != null) { /* we have the lock! */ }  
}  
// dispose releases the lock if we took it  
```

```csharp
public Medallion.Threading.Oracle.OracleDistributedReaderWriterLockHandle? TryAcquireWriteLock(System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.Oracle.OracleDistributedReaderWriterLock.TryAcquireWriteLock(System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

How long to wait before giving up on the acquisition attempt. Defaults to 0

<a name='Medallion.Threading.Oracle.OracleDistributedReaderWriterLock.TryAcquireWriteLock(System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [TryAcquireWriteLock(TimeSpan, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedReaderWriterLock.TryAcquireWriteLock.ypAYPzEP3B1U6LcOEQzWBw.md 'Medallion.Threading.IDistributedReaderWriterLock.TryAcquireWriteLock(System.TimeSpan,System.Threading.CancellationToken)')

#### Returns
[OracleDistributedReaderWriterLockHandle](OracleDistributedReaderWriterLockHandle.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLockHandle')  
An [OracleDistributedReaderWriterLockHandle](OracleDistributedReaderWriterLockHandle.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLockHandle') which can be used to release the lock or null on failure