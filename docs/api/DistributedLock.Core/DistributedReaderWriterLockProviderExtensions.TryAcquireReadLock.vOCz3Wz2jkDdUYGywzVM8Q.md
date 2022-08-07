#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading').[DistributedReaderWriterLockProviderExtensions](DistributedReaderWriterLockProviderExtensions.md 'Medallion.Threading.DistributedReaderWriterLockProviderExtensions')

## DistributedReaderWriterLockProviderExtensions.TryAcquireReadLock(this IDistributedReaderWriterLockProvider, string, TimeSpan, CancellationToken) Method

Equivalent to calling [CreateReaderWriterLock(string)](IDistributedReaderWriterLockProvider.CreateReaderWriterLock.BJyxJJllIyIqdlfqBHLDTA.md 'Medallion.Threading.IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)') and then  
[TryAcquireReadLock(TimeSpan, CancellationToken)](IDistributedReaderWriterLock.TryAcquireReadLock.FwhFBAUmx9brWLKd6O1SSw.md 'Medallion.Threading.IDistributedReaderWriterLock.TryAcquireReadLock(System.TimeSpan, System.Threading.CancellationToken)').

```csharp
public static Medallion.Threading.IDistributedSynchronizationHandle? TryAcquireReadLock(this Medallion.Threading.IDistributedReaderWriterLockProvider provider, string name, System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.DistributedReaderWriterLockProviderExtensions.TryAcquireReadLock(thisMedallion.Threading.IDistributedReaderWriterLockProvider,string,System.TimeSpan,System.Threading.CancellationToken).provider'></a>

`provider` [IDistributedReaderWriterLockProvider](IDistributedReaderWriterLockProvider.md 'Medallion.Threading.IDistributedReaderWriterLockProvider')

<a name='Medallion.Threading.DistributedReaderWriterLockProviderExtensions.TryAcquireReadLock(thisMedallion.Threading.IDistributedReaderWriterLockProvider,string,System.TimeSpan,System.Threading.CancellationToken).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.DistributedReaderWriterLockProviderExtensions.TryAcquireReadLock(thisMedallion.Threading.IDistributedReaderWriterLockProvider,string,System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

<a name='Medallion.Threading.DistributedReaderWriterLockProviderExtensions.TryAcquireReadLock(thisMedallion.Threading.IDistributedReaderWriterLockProvider,string,System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

#### Returns
[IDistributedSynchronizationHandle](IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle')