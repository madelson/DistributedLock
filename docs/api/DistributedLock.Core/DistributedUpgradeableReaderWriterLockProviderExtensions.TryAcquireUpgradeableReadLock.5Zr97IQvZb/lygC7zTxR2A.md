#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading').[DistributedUpgradeableReaderWriterLockProviderExtensions](DistributedUpgradeableReaderWriterLockProviderExtensions.md 'Medallion.Threading.DistributedUpgradeableReaderWriterLockProviderExtensions')

## DistributedUpgradeableReaderWriterLockProviderExtensions.TryAcquireUpgradeableReadLock(this IDistributedUpgradeableReaderWriterLockProvider, string, TimeSpan, CancellationToken) Method

Equivalent to calling [CreateUpgradeableReaderWriterLock(string)](IDistributedUpgradeableReaderWriterLockProvider.CreateUpgradeableReaderWriterLock.CLmVtTtcnh6LtTDHkHXXtQ.md 'Medallion.Threading.IDistributedUpgradeableReaderWriterLockProvider.CreateUpgradeableReaderWriterLock(string)') and then
[TryAcquireUpgradeableReadLock(TimeSpan, CancellationToken)](IDistributedUpgradeableReaderWriterLock.TryAcquireUpgradeableReadLock.NcomTiK4v4VsrD5p8zrY6A.md 'Medallion.Threading.IDistributedUpgradeableReaderWriterLock.TryAcquireUpgradeableReadLock(System.TimeSpan, System.Threading.CancellationToken)').

```csharp
public static Medallion.Threading.IDistributedLockUpgradeableHandle? TryAcquireUpgradeableReadLock(this Medallion.Threading.IDistributedUpgradeableReaderWriterLockProvider provider, string name, System.TimeSpan timeout=default(System.TimeSpan), System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.DistributedUpgradeableReaderWriterLockProviderExtensions.TryAcquireUpgradeableReadLock(thisMedallion.Threading.IDistributedUpgradeableReaderWriterLockProvider,string,System.TimeSpan,System.Threading.CancellationToken).provider'></a>

`provider` [IDistributedUpgradeableReaderWriterLockProvider](IDistributedUpgradeableReaderWriterLockProvider.md 'Medallion.Threading.IDistributedUpgradeableReaderWriterLockProvider')

<a name='Medallion.Threading.DistributedUpgradeableReaderWriterLockProviderExtensions.TryAcquireUpgradeableReadLock(thisMedallion.Threading.IDistributedUpgradeableReaderWriterLockProvider,string,System.TimeSpan,System.Threading.CancellationToken).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.DistributedUpgradeableReaderWriterLockProviderExtensions.TryAcquireUpgradeableReadLock(thisMedallion.Threading.IDistributedUpgradeableReaderWriterLockProvider,string,System.TimeSpan,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

<a name='Medallion.Threading.DistributedUpgradeableReaderWriterLockProviderExtensions.TryAcquireUpgradeableReadLock(thisMedallion.Threading.IDistributedUpgradeableReaderWriterLockProvider,string,System.TimeSpan,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

#### Returns
[IDistributedLockUpgradeableHandle](IDistributedLockUpgradeableHandle.md 'Medallion.Threading.IDistributedLockUpgradeableHandle')