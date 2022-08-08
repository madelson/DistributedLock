#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading').[IDistributedSemaphore](IDistributedSemaphore.md 'Medallion.Threading.IDistributedSemaphore')

## IDistributedSemaphore.TryAcquireAsync(TimeSpan, CancellationToken) Method

Attempts to acquire a semaphore ticket asynchronously. Usage: 

```csharp
await using (var handle = await mySemaphore.TryAcquireAsync(...))
{
    if (handle != null) { /* we have the ticket! */ }
}
// dispose releases the ticket if we took it
```

```csharp
System.Threading.Tasks.ValueTask<Medallion.Threading.IDistributedSynchronizationHandle?> TryAcquireAsync(System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.IDistributedSemaphore.TryAcquireAsync(System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

How long to wait before giving up on the acquisition attempt. Defaults to 0

<a name='Medallion.Threading.IDistributedSemaphore.TryAcquireAsync(System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

#### Returns
[System.Threading.Tasks.ValueTask&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')[IDistributedSynchronizationHandle](IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')  
An [IDistributedSynchronizationHandle](IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle') which can be used to release the ticket or null on failure