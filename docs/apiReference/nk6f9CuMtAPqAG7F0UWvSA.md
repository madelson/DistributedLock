### [Medallion.Threading.WaitHandles](0cv6wmZCIva5FK3cOR8t5g.md 'Medallion.Threading.WaitHandles').[WaitHandleDistributedSemaphore](u++QKv1+Gje9fkJlbjceow.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore')

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

Implements [TryAcquireAsync(TimeSpan, CancellationToken)](https://docs.microsoft.com/en-us/dotnet/api/Medallion.Threading.IDistributedSemaphore.TryAcquireAsync#Medallion_Threading_IDistributedSemaphore_TryAcquireAsync_System_TimeSpan,System_Threading_CancellationToken_ 'Medallion.Threading.IDistributedSemaphore.TryAcquireAsync(System.TimeSpan,System.Threading.CancellationToken)')

#### Returns
[System.Threading.Tasks.ValueTask&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')[WaitHandleDistributedSemaphoreHandle](Qzz0SP9O_CPbKNcEYp4rNQ.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphoreHandle')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')  
A [WaitHandleDistributedSemaphoreHandle](Qzz0SP9O_CPbKNcEYp4rNQ.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphoreHandle') which can be used to release the ticket or null on failure