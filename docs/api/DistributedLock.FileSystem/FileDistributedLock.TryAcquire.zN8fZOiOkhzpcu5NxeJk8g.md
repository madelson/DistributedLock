#### [DistributedLock.FileSystem](README.md 'README')
### [Medallion.Threading.FileSystem](Medallion.Threading.FileSystem.md 'Medallion.Threading.FileSystem').[FileDistributedLock](FileDistributedLock.md 'Medallion.Threading.FileSystem.FileDistributedLock')

## FileDistributedLock.TryAcquire(TimeSpan, CancellationToken) Method

Attempts to acquire the lock synchronously. Usage: 

```csharp
using (var handle = myLock.TryAcquire(...))
{
    if (handle != null) { /* we have the lock! */ }
}
// dispose releases the lock if we took it
```

```csharp
public Medallion.Threading.FileSystem.FileDistributedLockHandle? TryAcquire(System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.FileSystem.FileDistributedLock.TryAcquire(System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

How long to wait before giving up on the acquisition attempt. Defaults to 0

<a name='Medallion.Threading.FileSystem.FileDistributedLock.TryAcquire(System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [TryAcquire(TimeSpan, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLock.TryAcquire.GcM73KNvUAY5aoOOhgln1g.md 'Medallion.Threading.IDistributedLock.TryAcquire(System.TimeSpan,System.Threading.CancellationToken)')

#### Returns
[FileDistributedLockHandle](FileDistributedLockHandle.md 'Medallion.Threading.FileSystem.FileDistributedLockHandle')  
A [FileDistributedLockHandle](FileDistributedLockHandle.md 'Medallion.Threading.FileSystem.FileDistributedLockHandle') which can be used to release the lock or null on failure