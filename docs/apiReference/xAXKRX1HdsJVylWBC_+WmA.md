### [Medallion.Threading.WaitHandles](0cv6wmZCIva5FK3cOR8t5g.md 'Medallion.Threading.WaitHandles').[WaitHandleDistributedSemaphore](u++QKv1+Gje9fkJlbjceow.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore')

## WaitHandleDistributedSemaphore.TryAcquire(TimeSpan, CancellationToken) Method

Attempts to acquire a semaphore ticket synchronously. Usage:   
  
```csharp  
using (var handle = mySemaphore.TryAcquire(...))  
{  
    if (handle != null) { /* we have the ticket! */ }  
}  
// dispose releases the ticket if we took it  
```

```csharp
public Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphoreHandle? TryAcquire(System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.TryAcquire(System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

How long to wait before giving up on the acquisition attempt. Defaults to 0

<a name='Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore.TryAcquire(System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [TryAcquire(TimeSpan, CancellationToken)](https://docs.microsoft.com/en-us/dotnet/api/Medallion.Threading.IDistributedSemaphore.TryAcquire#Medallion_Threading_IDistributedSemaphore_TryAcquire_System_TimeSpan,System_Threading_CancellationToken_ 'Medallion.Threading.IDistributedSemaphore.TryAcquire(System.TimeSpan,System.Threading.CancellationToken)')

#### Returns
[WaitHandleDistributedSemaphoreHandle](Qzz0SP9O_CPbKNcEYp4rNQ.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphoreHandle')  
A [WaitHandleDistributedSemaphoreHandle](Qzz0SP9O_CPbKNcEYp4rNQ.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphoreHandle') which can be used to release the ticket or null on failure