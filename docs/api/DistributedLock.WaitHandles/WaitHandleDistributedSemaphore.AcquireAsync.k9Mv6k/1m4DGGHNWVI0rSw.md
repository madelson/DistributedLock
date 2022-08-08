#### [DistributedLock.WaitHandles](README.md 'README')
### [Medallion.Threading.WaitHandles](Medallion.Threading.WaitHandles.md 'Medallion.Threading.WaitHandles').[WaitHandleDistributedSemaphore](WaitHandleDistributedSemaphore.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore')

## WaitHandleDistributedSemaphore.AcquireAsync(Nullable<TimeSpan>, CancellationToken) Method

Acquires a semaphore ticket asynchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Usage: 

```csharp
await using (await mySemaphore.AcquireAsync(...))
{
    /* we have the ticket! */
}
// dispose releases the ticket
```

```csharp
public System.Threading.Tasks.ValueTask<Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphoreHandle> AcquireAsync(System.Nullable<System.TimeSpan> timeout=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.AcquireAsync(System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.Nullable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')[System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')

How long to wait before giving up on the acquisition attempt. Defaults to [System.Threading.Timeout.InfiniteTimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Timeout.InfiniteTimeSpan 'System.Threading.Timeout.InfiniteTimeSpan')

<a name='Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.AcquireAsync(System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [AcquireAsync(Nullable&lt;TimeSpan&gt;, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSemaphore.AcquireAsync.72hbd/OOOHUBoRAQHgD31Q.md 'Medallion.Threading.IDistributedSemaphore.AcquireAsync(System.Nullable{System.TimeSpan},System.Threading.CancellationToken)')

#### Returns
[System.Threading.Tasks.ValueTask&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')[WaitHandleDistributedSemaphoreHandle](WaitHandleDistributedSemaphoreHandle.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphoreHandle')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')  
A [WaitHandleDistributedSemaphoreHandle](WaitHandleDistributedSemaphoreHandle.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphoreHandle') which can be used to release the ticket