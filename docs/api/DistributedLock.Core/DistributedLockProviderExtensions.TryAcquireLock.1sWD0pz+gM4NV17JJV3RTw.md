#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading').[DistributedLockProviderExtensions](DistributedLockProviderExtensions.md 'Medallion.Threading.DistributedLockProviderExtensions')

## DistributedLockProviderExtensions.TryAcquireLock(this IDistributedLockProvider, string, TimeSpan, CancellationToken) Method

Equivalent to calling [CreateLock(string)](IDistributedLockProvider.CreateLock.lcl3dolUp9eZyeUENeHU9w.md 'Medallion.Threading.IDistributedLockProvider.CreateLock(string)') and then
[TryAcquire(TimeSpan, CancellationToken)](IDistributedLock.TryAcquire.GcM73KNvUAY5aoOOhgln1g.md 'Medallion.Threading.IDistributedLock.TryAcquire(System.TimeSpan, System.Threading.CancellationToken)').

```csharp
public static Medallion.Threading.IDistributedSynchronizationHandle? TryAcquireLock(this Medallion.Threading.IDistributedLockProvider provider, string name, System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.DistributedLockProviderExtensions.TryAcquireLock(thisMedallion.Threading.IDistributedLockProvider,string,System.TimeSpan,System.Threading.CancellationToken).provider'></a>

`provider` [IDistributedLockProvider](IDistributedLockProvider.md 'Medallion.Threading.IDistributedLockProvider')

<a name='Medallion.Threading.DistributedLockProviderExtensions.TryAcquireLock(thisMedallion.Threading.IDistributedLockProvider,string,System.TimeSpan,System.Threading.CancellationToken).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.DistributedLockProviderExtensions.TryAcquireLock(thisMedallion.Threading.IDistributedLockProvider,string,System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

<a name='Medallion.Threading.DistributedLockProviderExtensions.TryAcquireLock(thisMedallion.Threading.IDistributedLockProvider,string,System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

#### Returns
[IDistributedSynchronizationHandle](IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle')