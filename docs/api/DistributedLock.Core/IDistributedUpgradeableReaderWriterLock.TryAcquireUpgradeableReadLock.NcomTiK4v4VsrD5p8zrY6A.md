#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading').[IDistributedUpgradeableReaderWriterLock](IDistributedUpgradeableReaderWriterLock.md 'Medallion.Threading.IDistributedUpgradeableReaderWriterLock')

## IDistributedUpgradeableReaderWriterLock.TryAcquireUpgradeableReadLock(TimeSpan, CancellationToken) Method

Attempts to acquire an UPGRADE lock synchronously. Not compatible with another UPGRADE lock or a WRITE lock. Usage:   
  
```csharp  
using (var handle = myLock.TryAcquireUpgradeableReadLock(...))  
{  
    if (handle != null) { /* we have the lock! */ }  
}  
// dispose releases the lock if we took it  
```

```csharp
Medallion.Threading.IDistributedLockUpgradeableHandle? TryAcquireUpgradeableReadLock(System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.IDistributedUpgradeableReaderWriterLock.TryAcquireUpgradeableReadLock(System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

How long to wait before giving up on the acquisition attempt. Defaults to 0

<a name='Medallion.Threading.IDistributedUpgradeableReaderWriterLock.TryAcquireUpgradeableReadLock(System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

#### Returns
[IDistributedLockUpgradeableHandle](IDistributedLockUpgradeableHandle.md 'Medallion.Threading.IDistributedLockUpgradeableHandle')  
An [IDistributedLockUpgradeableHandle](IDistributedLockUpgradeableHandle.md 'Medallion.Threading.IDistributedLockUpgradeableHandle') which can be used to release the lock or null on failure