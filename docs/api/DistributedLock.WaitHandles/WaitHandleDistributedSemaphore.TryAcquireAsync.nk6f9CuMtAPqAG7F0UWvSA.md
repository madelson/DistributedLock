#### [DistributedLock.WaitHandles](README.md 'README')
### [Medallion.Threading.WaitHandles](Medallion.Threading.WaitHandles.md 'Medallion.Threading.WaitHandles').[WaitHandleDistributedSemaphore](WaitHandleDistributedSemaphore.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore')

## WaitHandleDistributedSemaphore.TryAcquireAsync(TimeSpan, CancellationToken) Method

Attempts to acquire a semaphore ticket asynchronously. Usage:   
  
```csharp  
await using (var handle = await mySemaphore.TryAcquireAsync(...))  
{  
    if (handle != null) { /* we have the ticket! */ }  
}  
// dispose releases the ticket if we took it  
```

```csharp
public System.Threading.Tasks.ValueTask<Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphoreHandle?> TryAcquireAsync(System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.TryAcquireAsync(System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

How long to wait before giving up on the acquisition attempt. Defaults to 0

<a name='Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.TryAcquireAsync(System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [TryAcquireAsync(TimeSpan, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSemaphore.TryAcquireAsync.yTpJMeiQTyO40ByV0nmdkQ.md 'Medallion.Threading.IDistributedSemaphore.TryAcquireAsync(System.TimeSpan,System.Threading.CancellationToken)')

#### Returns
[System.Threading.Tasks.ValueTask&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')[WaitHandleDistributedSemaphoreHandle](WaitHandleDistributedSemaphoreHandle.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphoreHandle')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')  
A [WaitHandleDistributedSemaphoreHandle](WaitHandleDistributedSemaphoreHandle.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphoreHandle') which can be used to release the ticket or null on failure