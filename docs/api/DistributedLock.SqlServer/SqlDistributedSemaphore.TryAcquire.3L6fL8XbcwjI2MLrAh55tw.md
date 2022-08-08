#### [DistributedLock.SqlServer](README.md 'README')
### [Medallion.Threading.SqlServer](Medallion.Threading.SqlServer.md 'Medallion.Threading.SqlServer').[SqlDistributedSemaphore](SqlDistributedSemaphore.md 'Medallion.Threading.SqlServer.SqlDistributedSemaphore')

## SqlDistributedSemaphore.TryAcquire(TimeSpan, CancellationToken) Method

Attempts to acquire a semaphore ticket synchronously. Usage: 

```csharp
using (var handle = mySemaphore.TryAcquire(...))
{
    if (handle != null) { /* we have the ticket! */ }
}
// dispose releases the ticket if we took it
```

```csharp
public Medallion.Threading.SqlServer.SqlDistributedSemaphoreHandle? TryAcquire(System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.SqlServer.SqlDistributedSemaphore.TryAcquire(System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

How long to wait before giving up on the acquisition attempt. Defaults to 0

<a name='Medallion.Threading.SqlServer.SqlDistributedSemaphore.TryAcquire(System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [TryAcquire(TimeSpan, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSemaphore.TryAcquire.G9QqgKI96XBtpNQoUp0RZg.md 'Medallion.Threading.IDistributedSemaphore.TryAcquire(System.TimeSpan,System.Threading.CancellationToken)')

#### Returns
[SqlDistributedSemaphoreHandle](SqlDistributedSemaphoreHandle.md 'Medallion.Threading.SqlServer.SqlDistributedSemaphoreHandle')  
A [SqlDistributedSemaphoreHandle](SqlDistributedSemaphoreHandle.md 'Medallion.Threading.SqlServer.SqlDistributedSemaphoreHandle') which can be used to release the ticket or null on failure