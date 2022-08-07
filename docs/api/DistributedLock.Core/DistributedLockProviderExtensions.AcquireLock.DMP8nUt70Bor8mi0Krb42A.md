#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading').[DistributedLockProviderExtensions](DistributedLockProviderExtensions.md 'Medallion.Threading.DistributedLockProviderExtensions')

## DistributedLockProviderExtensions.AcquireLock(this IDistributedLockProvider, string, Nullable<TimeSpan>, CancellationToken) Method

Equivalent to calling [CreateLock(string)](IDistributedLockProvider.CreateLock.lcl3dolUp9eZyeUENeHU9w.md 'Medallion.Threading.IDistributedLockProvider.CreateLock(string)') and then  
[Acquire(Nullable&lt;TimeSpan&gt;, CancellationToken)](IDistributedLock.Acquire.Q+8FXimBZqUrDv5tTRw59w.md 'Medallion.Threading.IDistributedLock.Acquire(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)').

```csharp
public static Medallion.Threading.IDistributedSynchronizationHandle AcquireLock(this Medallion.Threading.IDistributedLockProvider provider, string name, System.Nullable<System.TimeSpan> timeout=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.DistributedLockProviderExtensions.AcquireLock(thisMedallion.Threading.IDistributedLockProvider,string,System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).provider'></a>

`provider` [IDistributedLockProvider](IDistributedLockProvider.md 'Medallion.Threading.IDistributedLockProvider')

<a name='Medallion.Threading.DistributedLockProviderExtensions.AcquireLock(thisMedallion.Threading.IDistributedLockProvider,string,System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.DistributedLockProviderExtensions.AcquireLock(thisMedallion.Threading.IDistributedLockProvider,string,System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.Nullable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')[System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')

<a name='Medallion.Threading.DistributedLockProviderExtensions.AcquireLock(thisMedallion.Threading.IDistributedLockProvider,string,System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

#### Returns
[IDistributedSynchronizationHandle](IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle')