### [Medallion.Threading.WaitHandles](0cv6wmZCIva5FK3cOR8t5g.md 'Medallion.Threading.WaitHandles').[EventWaitHandleDistributedLock](LMZafb40QNKPqmMgiWZRCw.md 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock')

## EventWaitHandleDistributedLock.TryAcquireAsync(TimeSpan, CancellationToken) Method

Attempts to acquire the lock asynchronously. Usage:   
  
```csharp  
await using (var handle = await myLock.TryAcquireAsync(...))  
{  
    if (handle != null) { /* we have the lock! */ }  
}  
// dispose releases the lock if we took it  
```

```csharp
public System.Threading.Tasks.ValueTask<Medallion.Threading.WaitHandles.EventWaitHandleDistributedLockHandle?> TryAcquireAsync(System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.TryAcquireAsync(System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

How long to wait before giving up on the acquisition attempt. Defaults to 0

<a name='Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.TryAcquireAsync(System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [TryAcquireAsync(TimeSpan, CancellationToken)](https://docs.microsoft.com/en-us/dotnet/api/Medallion.Threading.IDistributedLock.TryAcquireAsync#Medallion_Threading_IDistributedLock_TryAcquireAsync_System_TimeSpan,System_Threading_CancellationToken_ 'Medallion.Threading.IDistributedLock.TryAcquireAsync(System.TimeSpan,System.Threading.CancellationToken)')

#### Returns
[System.Threading.Tasks.ValueTask&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')[EventWaitHandleDistributedLockHandle](ZmMZk+Ogw3TEy3Qj+BHNOg.md 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLockHandle')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')  
An [EventWaitHandleDistributedLockHandle](ZmMZk+Ogw3TEy3Qj+BHNOg.md 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLockHandle') which can be used to release the lock or null on failure