#### [DistributedLock.SqlServer](README.md 'README')
### [Medallion.Threading.SqlServer](Medallion.Threading.SqlServer.md 'Medallion.Threading.SqlServer').[SqlDistributedSemaphore](SqlDistributedSemaphore.md 'Medallion.Threading.SqlServer.SqlDistributedSemaphore')

## SqlDistributedSemaphore(string, int, IDbConnection) Constructor

Creates a semaphore with name [name](SqlDistributedSemaphore..ctor.BXWZBo51Xli5HcVudj4PPQ.md#Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string,int,System.Data.IDbConnection).name 'Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string, int, System.Data.IDbConnection).name') that can be acquired up to [maxCount](SqlDistributedSemaphore..ctor.BXWZBo51Xli5HcVudj4PPQ.md#Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string,int,System.Data.IDbConnection).maxCount 'Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string, int, System.Data.IDbConnection).maxCount')   
times concurrently. When acquired, the semaphore will be scoped to the given [connection](SqlDistributedSemaphore..ctor.BXWZBo51Xli5HcVudj4PPQ.md#Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string,int,System.Data.IDbConnection).connection 'Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string, int, System.Data.IDbConnection).connection').   
The [connection](SqlDistributedSemaphore..ctor.BXWZBo51Xli5HcVudj4PPQ.md#Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string,int,System.Data.IDbConnection).connection 'Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string, int, System.Data.IDbConnection).connection') is assumed to be externally managed: the [SqlDistributedSemaphore](SqlDistributedSemaphore.md 'Medallion.Threading.SqlServer.SqlDistributedSemaphore') will   
not attempt to open, close, or dispose it

```csharp
public SqlDistributedSemaphore(string name, int maxCount, System.Data.IDbConnection connection);
```
#### Parameters

<a name='Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string,int,System.Data.IDbConnection).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string,int,System.Data.IDbConnection).maxCount'></a>

`maxCount` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')

<a name='Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string,int,System.Data.IDbConnection).connection'></a>

`connection` [System.Data.IDbConnection](https://docs.microsoft.com/en-us/dotnet/api/System.Data.IDbConnection 'System.Data.IDbConnection')