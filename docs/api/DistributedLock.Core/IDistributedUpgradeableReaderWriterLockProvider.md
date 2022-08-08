#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading')

## IDistributedUpgradeableReaderWriterLockProvider Interface

Acts as a factory for [IDistributedUpgradeableReaderWriterLock](IDistributedUpgradeableReaderWriterLock.md 'Medallion.Threading.IDistributedUpgradeableReaderWriterLock') instances of a certain type. This interface may be
easier to use than [IDistributedUpgradeableReaderWriterLock](IDistributedUpgradeableReaderWriterLock.md 'Medallion.Threading.IDistributedUpgradeableReaderWriterLock') in dependency injection scenarios.

```csharp
public interface IDistributedUpgradeableReaderWriterLockProvider :
Medallion.Threading.IDistributedReaderWriterLockProvider
```

Implements [IDistributedReaderWriterLockProvider](IDistributedReaderWriterLockProvider.md 'Medallion.Threading.IDistributedReaderWriterLockProvider')

| Methods | |
| :--- | :--- |
| [CreateUpgradeableReaderWriterLock(string)](IDistributedUpgradeableReaderWriterLockProvider.CreateUpgradeableReaderWriterLock.CLmVtTtcnh6LtTDHkHXXtQ.md 'Medallion.Threading.IDistributedUpgradeableReaderWriterLockProvider.CreateUpgradeableReaderWriterLock(string)') | Constructs an [IDistributedUpgradeableReaderWriterLock](IDistributedUpgradeableReaderWriterLock.md 'Medallion.Threading.IDistributedUpgradeableReaderWriterLock') instance with the given [name](IDistributedUpgradeableReaderWriterLockProvider.CreateUpgradeableReaderWriterLock.CLmVtTtcnh6LtTDHkHXXtQ.md#Medallion.Threading.IDistributedUpgradeableReaderWriterLockProvider.CreateUpgradeableReaderWriterLock(string).name 'Medallion.Threading.IDistributedUpgradeableReaderWriterLockProvider.CreateUpgradeableReaderWriterLock(string).name'). |
