#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading').[DistributedLockProviderExtensions](DistributedLockProviderExtensions.md 'Medallion.Threading.DistributedLockProviderExtensions')

## DistributedLockProviderExtensions.TryAcquireLockAsync(this IDistributedLockProvider, string, TimeSpan, CancellationToken) Method

Equivalent to calling [CreateLock(string)](IDistributedLockProvider.CreateLock.lcl3dolUp9eZyeUENeHU9w.md 'Medallion.Threading.IDistributedLockProvider.CreateLock(string)') and then  
[TryAcquireAsync(TimeSpan, CancellationToken)](IDistributedLock.TryAcquireAsync.ZLhweq3GadK5OwGmTwruEQ.md 'Medallion.Threading.IDistributedLock.TryAcquireAsync(System.TimeSpan, System.Threading.CancellationToken)').

```csharp
public static System.Threading.Tasks.ValueTask<Medallion.Threading.IDistributedSynchronizationHandle?> TryAcquireLockAsync(this Medallion.Threading.IDistributedLockProvider provider, string name, System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.DistributedLockProviderExtensions.TryAcquireLockAsync(thisMedallion.Threading.IDistributedLockProvider,string,System.TimeSpan,System.Threading.CancellationToken).provider'></a>

`provider` [IDistributedLockProvider](IDistributedLockProvider.md 'Medallion.Threading.IDistributedLockProvider')

<a name='Medallion.Threading.DistributedLockProviderExtensions.TryAcquireLockAsync(thisMedallion.Threading.IDistributedLockProvider,string,System.TimeSpan,System.Threading.CancellationToken).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.DistributedLockProviderExtensions.TryAcquireLockAsync(thisMedallion.Threading.IDistributedLockProvider,string,System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

<a name='Medallion.Threading.DistributedLockProviderExtensions.TryAcquireLockAsync(thisMedallion.Threading.IDistributedLockProvider,string,System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

#### Returns
[System.Threading.Tasks.ValueTask&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')[IDistributedSynchronizationHandle](IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')