#### [DistributedLock.Redis](README.md 'README')
### [Medallion.Threading.Redis](Medallion.Threading.Redis.md 'Medallion.Threading.Redis').[RedisDistributedLock](RedisDistributedLock.md 'Medallion.Threading.Redis.RedisDistributedLock')

## RedisDistributedLock.TryAcquireAsync(TimeSpan, CancellationToken) Method

Attempts to acquire the lock asynchronously. Usage:   
  
```csharp  
await using (var handle = await myLock.TryAcquireAsync(...))  
{  
    if (handle != null) { /* we have the lock! */ }  
}  
// dispose releases the lock if we took it  
```

```csharp
public System.Threading.Tasks.ValueTask<Medallion.Threading.Redis.RedisDistributedLockHandle?> TryAcquireAsync(System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.Redis.RedisDistributedLock.TryAcquireAsync(System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

How long to wait before giving up on the acquisition attempt. Defaults to 0

<a name='Medallion.Threading.Redis.RedisDistributedLock.TryAcquireAsync(System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [TryAcquireAsync(TimeSpan, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLock.TryAcquireAsync.ZLhweq3GadK5OwGmTwruEQ.md 'Medallion.Threading.IDistributedLock.TryAcquireAsync(System.TimeSpan,System.Threading.CancellationToken)')

#### Returns
[System.Threading.Tasks.ValueTask&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')[RedisDistributedLockHandle](RedisDistributedLockHandle.md 'Medallion.Threading.Redis.RedisDistributedLockHandle')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')  
A [RedisDistributedLockHandle](RedisDistributedLockHandle.md 'Medallion.Threading.Redis.RedisDistributedLockHandle') which can be used to release the lock or null on failure