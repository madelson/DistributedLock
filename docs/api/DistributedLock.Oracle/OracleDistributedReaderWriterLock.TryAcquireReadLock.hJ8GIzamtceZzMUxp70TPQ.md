#### [DistributedLock.Oracle](README.md 'README')
### [Medallion.Threading.Oracle](Medallion.Threading.Oracle.md 'Medallion.Threading.Oracle').[OracleDistributedReaderWriterLock](OracleDistributedReaderWriterLock.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLock')

## OracleDistributedReaderWriterLock.TryAcquireReadLock(TimeSpan, CancellationToken) Method

Attempts to acquire a READ lock synchronously. Multiple readers are allowed. Not compatible with a WRITE lock. Usage:   
  
```csharp  
using (var handle = myLock.TryAcquireReadLock(...))  
{  
    if (handle != null) { /* we have the lock! */ }  
}  
// dispose releases the lock if we took it  
```

```csharp
public Medallion.Threading.Oracle.OracleDistributedReaderWriterLockHandle? TryAcquireReadLock(System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.Oracle.OracleDistributedReaderWriterLock.TryAcquireReadLock(System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

How long to wait before giving up on the acquisition attempt. Defaults to 0

<a name='Medallion.Threading.Oracle.OracleDistributedReaderWriterLock.TryAcquireReadLock(System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [TryAcquireReadLock(TimeSpan, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedReaderWriterLock.TryAcquireReadLock.FwhFBAUmx9brWLKd6O1SSw.md 'Medallion.Threading.IDistributedReaderWriterLock.TryAcquireReadLock(System.TimeSpan,System.Threading.CancellationToken)')

#### Returns
[OracleDistributedReaderWriterLockHandle](OracleDistributedReaderWriterLockHandle.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLockHandle')  
An [OracleDistributedReaderWriterLockHandle](OracleDistributedReaderWriterLockHandle.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLockHandle') which can be used to release the lock or null on failure