#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading').[DistributedReaderWriterLockProviderExtensions](DistributedReaderWriterLockProviderExtensions.md 'Medallion.Threading.DistributedReaderWriterLockProviderExtensions')

## DistributedReaderWriterLockProviderExtensions.TryAcquireReadLockAsync(this IDistributedReaderWriterLockProvider, string, TimeSpan, CancellationToken) Method

Equivalent to calling [CreateReaderWriterLock(string)](IDistributedReaderWriterLockProvider.CreateReaderWriterLock.BJyxJJllIyIqdlfqBHLDTA.md 'Medallion.Threading.IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)') and then  
[TryAcquireReadLockAsync(TimeSpan, CancellationToken)](IDistributedReaderWriterLock.TryAcquireReadLockAsync.1wx2S+CeVe62/fKwnr3rNQ.md 'Medallion.Threading.IDistributedReaderWriterLock.TryAcquireReadLockAsync(System.TimeSpan, System.Threading.CancellationToken)').

```csharp
public static System.Threading.Tasks.ValueTask<Medallion.Threading.IDistributedSynchronizationHandle?> TryAcquireReadLockAsync(this Medallion.Threading.IDistributedReaderWriterLockProvider provider, string name, System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.DistributedReaderWriterLockProviderExtensions.TryAcquireReadLockAsync(thisMedallion.Threading.IDistributedReaderWriterLockProvider,string,System.TimeSpan,System.Threading.CancellationToken).provider'></a>

`provider` [IDistributedReaderWriterLockProvider](IDistributedReaderWriterLockProvider.md 'Medallion.Threading.IDistributedReaderWriterLockProvider')

<a name='Medallion.Threading.DistributedReaderWriterLockProviderExtensions.TryAcquireReadLockAsync(thisMedallion.Threading.IDistributedReaderWriterLockProvider,string,System.TimeSpan,System.Threading.CancellationToken).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.DistributedReaderWriterLockProviderExtensions.TryAcquireReadLockAsync(thisMedallion.Threading.IDistributedReaderWriterLockProvider,string,System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

<a name='Medallion.Threading.DistributedReaderWriterLockProviderExtensions.TryAcquireReadLockAsync(thisMedallion.Threading.IDistributedReaderWriterLockProvider,string,System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

#### Returns
[System.Threading.Tasks.ValueTask&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')[IDistributedSynchronizationHandle](IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')