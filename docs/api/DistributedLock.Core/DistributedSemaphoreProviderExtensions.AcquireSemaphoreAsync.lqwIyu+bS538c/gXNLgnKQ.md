#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading').[DistributedSemaphoreProviderExtensions](DistributedSemaphoreProviderExtensions.md 'Medallion.Threading.DistributedSemaphoreProviderExtensions')

## DistributedSemaphoreProviderExtensions.AcquireSemaphoreAsync(this IDistributedSemaphoreProvider, string, int, Nullable<TimeSpan>, CancellationToken) Method

Equivalent to calling [CreateSemaphore(string, int)](IDistributedSemaphoreProvider.CreateSemaphore.AA9FahTKczyqDQd0GIAGzQ.md 'Medallion.Threading.IDistributedSemaphoreProvider.CreateSemaphore(string, int)') and then
[AcquireAsync(Nullable&lt;TimeSpan&gt;, CancellationToken)](IDistributedSemaphore.AcquireAsync.72hbd/OOOHUBoRAQHgD31Q.md 'Medallion.Threading.IDistributedSemaphore.AcquireAsync(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)').

```csharp
public static System.Threading.Tasks.ValueTask<Medallion.Threading.IDistributedSynchronizationHandle> AcquireSemaphoreAsync(this Medallion.Threading.IDistributedSemaphoreProvider provider, string name, int maxCount, System.Nullable<System.TimeSpan> timeout=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.DistributedSemaphoreProviderExtensions.AcquireSemaphoreAsync(thisMedallion.Threading.IDistributedSemaphoreProvider,string,int,System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).provider'></a>

`provider` [IDistributedSemaphoreProvider](IDistributedSemaphoreProvider.md 'Medallion.Threading.IDistributedSemaphoreProvider')

<a name='Medallion.Threading.DistributedSemaphoreProviderExtensions.AcquireSemaphoreAsync(thisMedallion.Threading.IDistributedSemaphoreProvider,string,int,System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.DistributedSemaphoreProviderExtensions.AcquireSemaphoreAsync(thisMedallion.Threading.IDistributedSemaphoreProvider,string,int,System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).maxCount'></a>

`maxCount` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')

<a name='Medallion.Threading.DistributedSemaphoreProviderExtensions.AcquireSemaphoreAsync(thisMedallion.Threading.IDistributedSemaphoreProvider,string,int,System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.Nullable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')[System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')

<a name='Medallion.Threading.DistributedSemaphoreProviderExtensions.AcquireSemaphoreAsync(thisMedallion.Threading.IDistributedSemaphoreProvider,string,int,System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

#### Returns
[System.Threading.Tasks.ValueTask&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')[IDistributedSynchronizationHandle](IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')