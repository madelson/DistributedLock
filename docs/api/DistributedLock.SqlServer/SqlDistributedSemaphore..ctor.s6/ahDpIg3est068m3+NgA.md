#### [DistributedLock.SqlServer](README.md 'README')
### [Medallion.Threading.SqlServer](Medallion.Threading.SqlServer.md 'Medallion.Threading.SqlServer').[SqlDistributedSemaphore](SqlDistributedSemaphore.md 'Medallion.Threading.SqlServer.SqlDistributedSemaphore')

## SqlDistributedSemaphore(string, int, string, Action<SqlConnectionOptionsBuilder>) Constructor

Creates a semaphore with name [name](SqlDistributedSemaphore..ctor.s6/ahDpIg3est068m3+NgA.md#Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string,int,string,System.Action_Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder_).name 'Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string, int, string, System.Action<Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder>).name') that can be acquired up to [maxCount](SqlDistributedSemaphore..ctor.s6/ahDpIg3est068m3+NgA.md#Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string,int,string,System.Action_Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder_).maxCount 'Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string, int, string, System.Action<Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder>).maxCount')   
times concurrently. The provided [connectionString](SqlDistributedSemaphore..ctor.s6/ahDpIg3est068m3+NgA.md#Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string,int,string,System.Action_Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder_).connectionString 'Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string, int, string, System.Action<Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder>).connectionString') will be used to connect to the database.

```csharp
public SqlDistributedSemaphore(string name, int maxCount, string connectionString, System.Action<Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder>? options=null);
```
#### Parameters

<a name='Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string,int,string,System.Action_Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder_).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string,int,string,System.Action_Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder_).maxCount'></a>

`maxCount` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')

<a name='Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string,int,string,System.Action_Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder_).connectionString'></a>

`connectionString` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string,int,string,System.Action_Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder_).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[SqlConnectionOptionsBuilder](SqlConnectionOptionsBuilder.md 'Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')