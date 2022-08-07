#### [DistributedLock.WaitHandles](README.md 'README')
### [Medallion.Threading.WaitHandles](Medallion.Threading.WaitHandles.md 'Medallion.Threading.WaitHandles').[WaitHandleDistributedSynchronizationProvider](WaitHandleDistributedSynchronizationProvider.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider')

## WaitHandleDistributedSynchronizationProvider.CreateSemaphore(string, int, bool) Method

Creates a [WaitHandleDistributedSemaphore](WaitHandleDistributedSemaphore.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore') with the given [name](WaitHandleDistributedSynchronizationProvider.CreateSemaphore.lScE6Nm5quO4w5D+oiHGrA.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateSemaphore(string,int,bool).name 'Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateSemaphore(string, int, bool).name')  
and [maxCount](WaitHandleDistributedSynchronizationProvider.CreateSemaphore.lScE6Nm5quO4w5D+oiHGrA.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateSemaphore(string,int,bool).maxCount 'Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateSemaphore(string, int, bool).maxCount'). Unless [exactName](WaitHandleDistributedSynchronizationProvider.CreateSemaphore.lScE6Nm5quO4w5D+oiHGrA.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateSemaphore(string,int,bool).exactName 'Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateSemaphore(string, int, bool).exactName') is specified, invalid wait   
handle names will be escaped/hashed.

```csharp
public Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore CreateSemaphore(string name, int maxCount, bool exactName=false);
```
#### Parameters

<a name='Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateSemaphore(string,int,bool).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateSemaphore(string,int,bool).maxCount'></a>

`maxCount` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')

<a name='Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateSemaphore(string,int,bool).exactName'></a>

`exactName` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')

#### Returns
[WaitHandleDistributedSemaphore](WaitHandleDistributedSemaphore.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore')