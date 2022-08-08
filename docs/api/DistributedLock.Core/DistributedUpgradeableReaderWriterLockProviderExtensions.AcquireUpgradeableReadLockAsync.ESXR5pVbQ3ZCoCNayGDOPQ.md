#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading').[DistributedUpgradeableReaderWriterLockProviderExtensions](DistributedUpgradeableReaderWriterLockProviderExtensions.md 'Medallion.Threading.DistributedUpgradeableReaderWriterLockProviderExtensions')

## DistributedUpgradeableReaderWriterLockProviderExtensions.AcquireUpgradeableReadLockAsync(this IDistributedUpgradeableReaderWriterLockProvider, string, Nullable<TimeSpan>, CancellationToken) Method

Equivalent to calling [CreateUpgradeableReaderWriterLock(string)](IDistributedUpgradeableReaderWriterLockProvider.CreateUpgradeableReaderWriterLock.CLmVtTtcnh6LtTDHkHXXtQ.md 'Medallion.Threading.IDistributedUpgradeableReaderWriterLockProvider.CreateUpgradeableReaderWriterLock(string)') and then
[AcquireUpgradeableReadLockAsync(Nullable&lt;TimeSpan&gt;, CancellationToken)](IDistributedUpgradeableReaderWriterLock.AcquireUpgradeableReadLockAsync.XDD/LbfIJrScMNU14D5OvA.md 'Medallion.Threading.IDistributedUpgradeableReaderWriterLock.AcquireUpgradeableReadLockAsync(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)').

```csharp
public static System.Threading.Tasks.ValueTask<Medallion.Threading.IDistributedLockUpgradeableHandle> AcquireUpgradeableReadLockAsync(this Medallion.Threading.IDistributedUpgradeableReaderWriterLockProvider provider, string name, System.Nullable<System.TimeSpan> timeout=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.DistributedUpgradeableReaderWriterLockProviderExtensions.AcquireUpgradeableReadLockAsync(thisMedallion.Threading.IDistributedUpgradeableReaderWriterLockProvider,string,System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).provider'></a>

`provider` [IDistributedUpgradeableReaderWriterLockProvider](IDistributedUpgradeableReaderWriterLockProvider.md 'Medallion.Threading.IDistributedUpgradeableReaderWriterLockProvider')

<a name='Medallion.Threading.DistributedUpgradeableReaderWriterLockProviderExtensions.AcquireUpgradeableReadLockAsync(thisMedallion.Threading.IDistributedUpgradeableReaderWriterLockProvider,string,System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.DistributedUpgradeableReaderWriterLockProviderExtensions.AcquireUpgradeableReadLockAsync(thisMedallion.Threading.IDistributedUpgradeableReaderWriterLockProvider,string,System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.Nullable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')[System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')

<a name='Medallion.Threading.DistributedUpgradeableReaderWriterLockProviderExtensions.AcquireUpgradeableReadLockAsync(thisMedallion.Threading.IDistributedUpgradeableReaderWriterLockProvider,string,System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

#### Returns
[System.Threading.Tasks.ValueTask&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')[IDistributedLockUpgradeableHandle](IDistributedLockUpgradeableHandle.md 'Medallion.Threading.IDistributedLockUpgradeableHandle')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')