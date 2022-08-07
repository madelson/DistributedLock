#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading')

## IDistributedLockProvider Interface

Acts as a factory for [IDistributedLock](IDistributedLock.md 'Medallion.Threading.IDistributedLock') instances of a certain type. This interface may be  
easier to use than [IDistributedLock](IDistributedLock.md 'Medallion.Threading.IDistributedLock') in dependency injection scenarios.

```csharp
public interface IDistributedLockProvider
```

| Methods | |
| :--- | :--- |
| [CreateLock(string)](IDistributedLockProvider.CreateLock.lcl3dolUp9eZyeUENeHU9w.md 'Medallion.Threading.IDistributedLockProvider.CreateLock(string)') | Constructs an [IDistributedLock](IDistributedLock.md 'Medallion.Threading.IDistributedLock') instance with the given [name](IDistributedLockProvider.CreateLock.lcl3dolUp9eZyeUENeHU9w.md#Medallion.Threading.IDistributedLockProvider.CreateLock(string).name 'Medallion.Threading.IDistributedLockProvider.CreateLock(string).name'). |
