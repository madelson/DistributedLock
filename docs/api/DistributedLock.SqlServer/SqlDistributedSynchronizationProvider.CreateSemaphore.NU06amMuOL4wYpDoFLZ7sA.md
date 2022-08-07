#### [DistributedLock.SqlServer](README.md 'README')
### [Medallion.Threading.SqlServer](Medallion.Threading.SqlServer.md 'Medallion.Threading.SqlServer').[SqlDistributedSynchronizationProvider](SqlDistributedSynchronizationProvider.md 'Medallion.Threading.SqlServer.SqlDistributedSynchronizationProvider')

## SqlDistributedSynchronizationProvider.CreateSemaphore(string, int) Method

Constructs an instance of [SqlDistributedSemaphore](SqlDistributedSemaphore.md 'Medallion.Threading.SqlServer.SqlDistributedSemaphore') with the provided [name](SqlDistributedSynchronizationProvider.CreateSemaphore.NU06amMuOL4wYpDoFLZ7sA.md#Medallion.Threading.SqlServer.SqlDistributedSynchronizationProvider.CreateSemaphore(string,int).name 'Medallion.Threading.SqlServer.SqlDistributedSynchronizationProvider.CreateSemaphore(string, int).name') and [maxCount](SqlDistributedSynchronizationProvider.CreateSemaphore.NU06amMuOL4wYpDoFLZ7sA.md#Medallion.Threading.SqlServer.SqlDistributedSynchronizationProvider.CreateSemaphore(string,int).maxCount 'Medallion.Threading.SqlServer.SqlDistributedSynchronizationProvider.CreateSemaphore(string, int).maxCount').

```csharp
public Medallion.Threading.SqlServer.SqlDistributedSemaphore CreateSemaphore(string name, int maxCount);
```
#### Parameters

<a name='Medallion.Threading.SqlServer.SqlDistributedSynchronizationProvider.CreateSemaphore(string,int).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.SqlServer.SqlDistributedSynchronizationProvider.CreateSemaphore(string,int).maxCount'></a>

`maxCount` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')

Implements [CreateSemaphore(string, int)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSemaphoreProvider.CreateSemaphore.AA9FahTKczyqDQd0GIAGzQ.md 'Medallion.Threading.IDistributedSemaphoreProvider.CreateSemaphore(System.String,System.Int32)')

#### Returns
[SqlDistributedSemaphore](SqlDistributedSemaphore.md 'Medallion.Threading.SqlServer.SqlDistributedSemaphore')