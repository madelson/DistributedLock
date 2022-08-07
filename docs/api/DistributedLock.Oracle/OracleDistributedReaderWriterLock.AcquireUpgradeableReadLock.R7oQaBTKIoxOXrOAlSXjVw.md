#### [DistributedLock.Oracle](README.md 'README')
### [Medallion.Threading.Oracle](Medallion.Threading.Oracle.md 'Medallion.Threading.Oracle').[OracleDistributedReaderWriterLock](OracleDistributedReaderWriterLock.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLock')

## OracleDistributedReaderWriterLock.AcquireUpgradeableReadLock(Nullable<TimeSpan>, CancellationToken) Method

Acquires an UPGRADE lock synchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Not compatible with another UPGRADE lock or a WRITE lock. Usage:   
  
```csharp  
using (myLock.AcquireUpgradeableReadLock(...))  
{  
    /* we have the lock! */  
}  
// dispose releases the lock  
```

```csharp
public Medallion.Threading.Oracle.OracleDistributedReaderWriterLockUpgradeableHandle AcquireUpgradeableReadLock(System.Nullable<System.TimeSpan> timeout=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.Oracle.OracleDistributedReaderWriterLock.AcquireUpgradeableReadLock(System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.Nullable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')[System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')

How long to wait before giving up on the acquisition attempt. Defaults to [System.Threading.Timeout.InfiniteTimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Timeout.InfiniteTimeSpan 'System.Threading.Timeout.InfiniteTimeSpan')

<a name='Medallion.Threading.Oracle.OracleDistributedReaderWriterLock.AcquireUpgradeableReadLock(System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [AcquireUpgradeableReadLock(Nullable&lt;TimeSpan&gt;, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedUpgradeableReaderWriterLock.AcquireUpgradeableReadLock.MgsbqNeNv0qen0RVQV8MHA.md 'Medallion.Threading.IDistributedUpgradeableReaderWriterLock.AcquireUpgradeableReadLock(System.Nullable{System.TimeSpan},System.Threading.CancellationToken)')

#### Returns
[OracleDistributedReaderWriterLockUpgradeableHandle](OracleDistributedReaderWriterLockUpgradeableHandle.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLockUpgradeableHandle')  
An [OracleDistributedReaderWriterLockUpgradeableHandle](OracleDistributedReaderWriterLockUpgradeableHandle.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLockUpgradeableHandle') which can be used to release the lock