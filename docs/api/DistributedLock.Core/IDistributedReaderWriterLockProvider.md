#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading')

## IDistributedReaderWriterLockProvider Interface

Acts as a factory for [IDistributedReaderWriterLock](IDistributedReaderWriterLock.md 'Medallion.Threading.IDistributedReaderWriterLock') instances of a certain type. This interface may be
easier to use than [IDistributedReaderWriterLock](IDistributedReaderWriterLock.md 'Medallion.Threading.IDistributedReaderWriterLock') in dependency injection scenarios.

```csharp
public interface IDistributedReaderWriterLockProvider
```

Derived  
&#8627; [IDistributedUpgradeableReaderWriterLockProvider](IDistributedUpgradeableReaderWriterLockProvider.md 'Medallion.Threading.IDistributedUpgradeableReaderWriterLockProvider')

| Methods | |
| :--- | :--- |
| [CreateReaderWriterLock(string)](IDistributedReaderWriterLockProvider.CreateReaderWriterLock.BJyxJJllIyIqdlfqBHLDTA.md 'Medallion.Threading.IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string)') | Constructs an [IDistributedReaderWriterLock](IDistributedReaderWriterLock.md 'Medallion.Threading.IDistributedReaderWriterLock') instance with the given [name](IDistributedReaderWriterLockProvider.CreateReaderWriterLock.BJyxJJllIyIqdlfqBHLDTA.md#Medallion.Threading.IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string).name 'Medallion.Threading.IDistributedReaderWriterLockProvider.CreateReaderWriterLock(string).name'). |
