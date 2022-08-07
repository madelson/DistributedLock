### [Medallion.Threading.WaitHandles](0cv6wmZCIva5FK3cOR8t5g.md 'Medallion.Threading.WaitHandles').[EventWaitHandleDistributedLock](LMZafb40QNKPqmMgiWZRCw.md 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock')

## EventWaitHandleDistributedLock.TryAcquire(TimeSpan, CancellationToken) Method

Attempts to acquire the lock synchronously. Usage:   
  
```csharp  
using (var handle = myLock.TryAcquire(...))  
{  
    if (handle != null) { /* we have the lock! */ }  
}  
// dispose releases the lock if we took it  
```

```csharp
public Medallion.Threading.WaitHandles.EventWaitHandleDistributedLockHandle? TryAcquire(System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.TryAcquire(System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

How long to wait before giving up on the acquisition attempt. Defaults to 0

<a name='Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.TryAcquire(System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [TryAcquire(TimeSpan, CancellationToken)](https://docs.microsoft.com/en-us/dotnet/api/Medallion.Threading.IDistributedLock.TryAcquire#Medallion_Threading_IDistributedLock_TryAcquire_System_TimeSpan,System_Threading_CancellationToken_ 'Medallion.Threading.IDistributedLock.TryAcquire(System.TimeSpan,System.Threading.CancellationToken)')

#### Returns
[EventWaitHandleDistributedLockHandle](ZmMZk+Ogw3TEy3Qj+BHNOg.md 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLockHandle')  
An [EventWaitHandleDistributedLockHandle](ZmMZk+Ogw3TEy3Qj+BHNOg.md 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLockHandle') which can be used to release the lock or null on failure