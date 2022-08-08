#### [DistributedLock.SqlServer](README.md 'README')
### [Medallion.Threading.SqlServer](Medallion.Threading.SqlServer.md 'Medallion.Threading.SqlServer').[SqlDistributedSemaphore](SqlDistributedSemaphore.md 'Medallion.Threading.SqlServer.SqlDistributedSemaphore')

## SqlDistributedSemaphore.TryAcquireAsync(TimeSpan, CancellationToken) Method

Attempts to acquire a semaphore ticket asynchronously. Usage: 

```csharp
await using (var handle = await mySemaphore.TryAcquireAsync(...))
{
    if (handle != null) { /* we have the ticket! */ }
}
// dispose releases the ticket if we took it
```

```csharp
public System.Threading.Tasks.ValueTask<Medallion.Threading.SqlServer.SqlDistributedSemaphoreHandle?> TryAcquireAsync(System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.SqlServer.SqlDistributedSemaphore.TryAcquireAsync(System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

How long to wait before giving up on the acquisition attempt. Defaults to 0

<a name='Medallion.Threading.SqlServer.SqlDistributedSemaphore.TryAcquireAsync(System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [TryAcquireAsync(TimeSpan, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSemaphore.TryAcquireAsync.yTpJMeiQTyO40ByV0nmdkQ.md 'Medallion.Threading.IDistributedSemaphore.TryAcquireAsync(System.TimeSpan,System.Threading.CancellationToken)')

#### Returns
[System.Threading.Tasks.ValueTask&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')[SqlDistributedSemaphoreHandle](SqlDistributedSemaphoreHandle.md 'Medallion.Threading.SqlServer.SqlDistributedSemaphoreHandle')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')  
A [SqlDistributedSemaphoreHandle](SqlDistributedSemaphoreHandle.md 'Medallion.Threading.SqlServer.SqlDistributedSemaphoreHandle') which can be used to release the ticket or null on failure