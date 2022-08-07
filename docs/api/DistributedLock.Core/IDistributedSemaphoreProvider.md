#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading')

## IDistributedSemaphoreProvider Interface

Acts as a factory for [IDistributedSemaphore](IDistributedSemaphore.md 'Medallion.Threading.IDistributedSemaphore') instances of a certain type. This interface may be  
easier to use than [IDistributedSemaphore](IDistributedSemaphore.md 'Medallion.Threading.IDistributedSemaphore') in dependency injection scenarios.

```csharp
public interface IDistributedSemaphoreProvider
```

| Methods | |
| :--- | :--- |
| [CreateSemaphore(string, int)](IDistributedSemaphoreProvider.CreateSemaphore.AA9FahTKczyqDQd0GIAGzQ.md 'Medallion.Threading.IDistributedSemaphoreProvider.CreateSemaphore(string, int)') | Constructs an [IDistributedSemaphore](IDistributedSemaphore.md 'Medallion.Threading.IDistributedSemaphore') instance with the given [name](IDistributedSemaphoreProvider.CreateSemaphore.AA9FahTKczyqDQd0GIAGzQ.md#Medallion.Threading.IDistributedSemaphoreProvider.CreateSemaphore(string,int).name 'Medallion.Threading.IDistributedSemaphoreProvider.CreateSemaphore(string, int).name'). |
