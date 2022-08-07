#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading').[DistributedReaderWriterLockProviderExtensions](DistributedReaderWriterLockProviderExtensions.md 'Medallion.Threading.DistributedReaderWriterLockProviderExtensions')

## DistributedReaderWriterLockProviderExtensions.AcquireWriteLock(this IDistributedReaderWriterLockProvider, string, Nullable<TimeSpan>, CancellationToken) Method

Equivalent to calling [CreateReaderWriterLock(string)](IDistributedReaderWriterLockProvider.CreateReaderWriterLock.BJyxJJllIyIqdlfqBHLDTA.md 'Medallion.Threading.IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)') and then  
[AcquireWriteLock(Nullable&lt;TimeSpan&gt;, CancellationToken)](IDistributedReaderWriterLock.AcquireWriteLock.8zDirrYDrgr0WzrsnH7blA.md 'Medallion.Threading.IDistributedReaderWriterLock.AcquireWriteLock(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)').

```csharp
public static Medallion.Threading.IDistributedSynchronizationHandle AcquireWriteLock(this Medallion.Threading.IDistributedReaderWriterLockProvider provider, string name, System.Nullable<System.TimeSpan> timeout=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.DistributedReaderWriterLockProviderExtensions.AcquireWriteLock(thisMedallion.Threading.IDistributedReaderWriterLockProvider,string,System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).provider'></a>

`provider` [IDistributedReaderWriterLockProvider](IDistributedReaderWriterLockProvider.md 'Medallion.Threading.IDistributedReaderWriterLockProvider')

<a name='Medallion.Threading.DistributedReaderWriterLockProviderExtensions.AcquireWriteLock(thisMedallion.Threading.IDistributedReaderWriterLockProvider,string,System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.DistributedReaderWriterLockProviderExtensions.AcquireWriteLock(thisMedallion.Threading.IDistributedReaderWriterLockProvider,string,System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.Nullable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')[System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')

<a name='Medallion.Threading.DistributedReaderWriterLockProviderExtensions.AcquireWriteLock(thisMedallion.Threading.IDistributedReaderWriterLockProvider,string,System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

#### Returns
[IDistributedSynchronizationHandle](IDistributedSynchronizationHandle.md 'Medallion.Threading.IDistributedSynchronizationHandle')