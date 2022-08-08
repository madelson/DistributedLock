#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading').[DistributedSemaphoreProviderExtensions](DistributedSemaphoreProviderExtensions.md 'Medallion.Threading.DistributedSemaphoreProviderExtensions')

## DistributedSemaphoreProviderExtensions.TryAcquireSemaphore(this IDistributedSemaphoreProvider, string, int, TimeSpan, CancellationToken) Method

Equivalent to calling [CreateSemaphore(string, int)](IDistributedSemaphoreProvider.CreateSemaphore.AA9FahTKczyqDQd0GIAGzQ.md 'Medallion.Threading.IDistributedSemaphoreProvider.CreateSemaphore(string, int)') and then
[TryAcquire(TimeSpan, CancellationToken)](IDistributedSemaphore.TryAcquire.G9QqgKI96XBtpNQoUp0RZg.md 'Medallion.Threading.IDistributedSemaphore.TryAcquire(System.TimeSpan, System.Threading.CancellationToken)').

```csharp
public static Medallion.Threading.IDistributedSynchronizationHandle? TryAcquireSemaphore(this Medallion.Threading.IDistributedSemaphoreProvider provider, string name, int maxCount, System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.DistributedSemaphoreProviderExtensions.TryAcquireSemaphore(thisMedallion.Threading.IDistributedSemaphoreProvider,string,int,System.TimeSpan,System.Threading.CancellationToken).provider'></a>

`provider` [IDistributedSemaphoreProvider](IDistributedSemaphoreProvider.md 'Medallion.Threading.IDistributedSemaphoreProvider')

<a name='Medallion.Threading.DistributedSemaphoreProviderExtensions.TryAcquireSemaphore(thisMedallion.Threading.IDistributedSemaphoreProvider,string,int,System.TimeSpan,System.Threading.CancellationToken).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.DistributedSemaphoreProviderExtensions.TryAcquireSemaphore(thisMedallion.Threading.IDistributedSemaphoreProvider,string,int,System.TimeSpan,System.Threading.CancellationToken).maxCount'></a>

`maxCount` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')

<a name='Medallion.Threading.DistributedSemaphoreProviderExtensions.TryAcquireSemaphore(thisMedallion.Threading.IDistributedSemaphoreProvider,string,int,System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

<a name='Medallion.Threading.DistributedSemaphoreProviderExtensions.TryAcquireSemaphore(thisMedallion.Threading.IDistributedSemaphoreProvider,string,int,System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

#### Returns
[IDistributedSynchronizationHandle](IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle')