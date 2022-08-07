### [Medallion.Threading.WaitHandles](0cv6wmZCIva5FK3cOR8t5g.md 'Medallion.Threading.WaitHandles').[WaitHandleDistributedSynchronizationProvider](UsS2hO+xXgwbwvK7JBT0hA.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider')

## WaitHandleDistributedSynchronizationProvider.CreateSemaphore(string, int, bool) Method

Creates a [WaitHandleDistributedSemaphore](u++QKv1+Gje9fkJlbjceow.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore') with the given [name](lScE6Nm5quO4w5D+oiHGrA.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateSemaphore(string,int,bool).name 'Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateSemaphore(string, int, bool).name')  
and [maxCount](lScE6Nm5quO4w5D+oiHGrA.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateSemaphore(string,int,bool).maxCount 'Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateSemaphore(string, int, bool).maxCount'). Unless [exactName](lScE6Nm5quO4w5D+oiHGrA.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateSemaphore(string,int,bool).exactName 'Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateSemaphore(string, int, bool).exactName') is specified, invalid wait   
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
[WaitHandleDistributedSemaphore](u++QKv1+Gje9fkJlbjceow.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore')