#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading').[DistributedSemaphoreProviderExtensions](DistributedSemaphoreProviderExtensions.md 'Medallion.Threading.DistributedSemaphoreProviderExtensions')

## DistributedSemaphoreProviderExtensions.AcquireSemaphore(this IDistributedSemaphoreProvider, string, int, Nullable<TimeSpan>, CancellationToken) Method

Equivalent to calling [CreateSemaphore(string, int)](IDistributedSemaphoreProvider.CreateSemaphore.AA9FahTKczyqDQd0GIAGzQ.md 'Medallion.Threading.IDistributedSemaphoreProvider.CreateSemaphore(string, int)') and then  
[Acquire(Nullable&lt;TimeSpan&gt;, CancellationToken)](IDistributedSemaphore.Acquire.Idy1BAzgGUWQ22QmqRZDsg.md 'Medallion.Threading.IDistributedSemaphore.Acquire(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)').

```csharp
public static Medallion.Threading.IDistributedSynchronizationHandle AcquireSemaphore(this Medallion.Threading.IDistributedSemaphoreProvider provider, string name, int maxCount, System.Nullable<System.TimeSpan> timeout=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.DistributedSemaphoreProviderExtensions.AcquireSemaphore(thisMedallion.Threading.IDistributedSemaphoreProvider,string,int,System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).provider'></a>

`provider` [IDistributedSemaphoreProvider](IDistributedSemaphoreProvider.md 'Medallion.Threading.IDistributedSemaphoreProvider')

<a name='Medallion.Threading.DistributedSemaphoreProviderExtensions.AcquireSemaphore(thisMedallion.Threading.IDistributedSemaphoreProvider,string,int,System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.DistributedSemaphoreProviderExtensions.AcquireSemaphore(thisMedallion.Threading.IDistributedSemaphoreProvider,string,int,System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).maxCount'></a>

`maxCount` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')

<a name='Medallion.Threading.DistributedSemaphoreProviderExtensions.AcquireSemaphore(thisMedallion.Threading.IDistributedSemaphoreProvider,string,int,System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.Nullable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')[System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')

<a name='Medallion.Threading.DistributedSemaphoreProviderExtensions.AcquireSemaphore(thisMedallion.Threading.IDistributedSemaphoreProvider,string,int,System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

#### Returns
[IDistributedSynchronizationHandle](IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle')