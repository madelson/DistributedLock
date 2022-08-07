#### [DistributedLock.FileSystem](README.md 'README')
### [Medallion.Threading.FileSystem](Medallion.Threading.FileSystem.md 'Medallion.Threading.FileSystem').[FileDistributedLock](FileDistributedLock.md 'Medallion.Threading.FileSystem.FileDistributedLock')

## FileDistributedLock.TryAcquireAsync(TimeSpan, CancellationToken) Method

Attempts to acquire the lock asynchronously. Usage:   
  
```csharp  
await using (var handle = await myLock.TryAcquireAsync(...))  
{  
    if (handle != null) { /* we have the lock! */ }  
}  
// dispose releases the lock if we took it  
```

```csharp
public System.Threading.Tasks.ValueTask<Medallion.Threading.FileSystem.FileDistributedLockHandle?> TryAcquireAsync(System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.FileSystem.FileDistributedLock.TryAcquireAsync(System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

How long to wait before giving up on the acquisition attempt. Defaults to 0

<a name='Medallion.Threading.FileSystem.FileDistributedLock.TryAcquireAsync(System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [TryAcquireAsync(TimeSpan, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLock.TryAcquireAsync.ZLhweq3GadK5OwGmTwruEQ.md 'Medallion.Threading.IDistributedLock.TryAcquireAsync(System.TimeSpan,System.Threading.CancellationToken)')

#### Returns
[System.Threading.Tasks.ValueTask&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')[FileDistributedLockHandle](FileDistributedLockHandle.md 'Medallion.Threading.FileSystem.FileDistributedLockHandle')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')  
A [FileDistributedLockHandle](FileDistributedLockHandle.md 'Medallion.Threading.FileSystem.FileDistributedLockHandle') which can be used to release the lock or null on failure