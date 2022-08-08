#### [DistributedLock.WaitHandles](README.md 'README')
### [Medallion.Threading.WaitHandles](Medallion.Threading.WaitHandles.md 'Medallion.Threading.WaitHandles').[WaitHandleDistributedSemaphore](WaitHandleDistributedSemaphore.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore')

## WaitHandleDistributedSemaphore.Acquire(Nullable<TimeSpan>, CancellationToken) Method

Acquires a semaphore ticket synchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Usage: 

```csharp
using (mySemaphore.Acquire(...))
{
    /* we have the ticket! */
}
// dispose releases the ticket
```

```csharp
public Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphoreHandle Acquire(System.Nullable<System.TimeSpan> timeout=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.Acquire(System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.Nullable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')[System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')

How long to wait before giving up on the acquisition attempt. Defaults to [System.Threading.Timeout.InfiniteTimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Timeout.InfiniteTimeSpan 'System.Threading.Timeout.InfiniteTimeSpan')

<a name='Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.Acquire(System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [Acquire(Nullable&lt;TimeSpan&gt;, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSemaphore.Acquire.Idy1BAzgGUWQ22QmqRZDsg.md 'Medallion.Threading.IDistributedSemaphore.Acquire(System.Nullable{System.TimeSpan},System.Threading.CancellationToken)')

#### Returns
[WaitHandleDistributedSemaphoreHandle](WaitHandleDistributedSemaphoreHandle.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphoreHandle')  
A [WaitHandleDistributedSemaphoreHandle](WaitHandleDistributedSemaphoreHandle.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphoreHandle') which can be used to release the ticket