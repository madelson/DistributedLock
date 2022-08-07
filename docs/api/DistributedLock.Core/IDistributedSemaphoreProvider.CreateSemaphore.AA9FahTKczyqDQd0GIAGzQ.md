#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading').[IDistributedSemaphoreProvider](IDistributedSemaphoreProvider.md 'Medallion.Threading.IDistributedSemaphoreProvider')

## IDistributedSemaphoreProvider.CreateSemaphore(string, int) Method

Constructs an [IDistributedSemaphore](IDistributedSemaphore.md 'Medallion.Threading.IDistributedSemaphore') instance with the given [name](IDistributedSemaphoreProvider.CreateSemaphore.AA9FahTKczyqDQd0GIAGzQ.md#Medallion.Threading.IDistributedSemaphoreProvider.CreateSemaphore(string,int).name 'Medallion.Threading.IDistributedSemaphoreProvider.CreateSemaphore(string, int).name').

```csharp
Medallion.Threading.IDistributedSemaphore CreateSemaphore(string name, int maxCount);
```
#### Parameters

<a name='Medallion.Threading.IDistributedSemaphoreProvider.CreateSemaphore(string,int).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.IDistributedSemaphoreProvider.CreateSemaphore(string,int).maxCount'></a>

`maxCount` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')

#### Returns
[IDistributedSemaphore](IDistributedSemaphore.md 'Medallion.Threading.IDistributedSemaphore')