#### [DistributedLock.SqlServer](README.md 'README')
### [Medallion.Threading.SqlServer](Medallion.Threading.SqlServer.md 'Medallion.Threading.SqlServer').[SqlDistributedSemaphore](SqlDistributedSemaphore.md 'Medallion.Threading.SqlServer.SqlDistributedSemaphore')

## SqlDistributedSemaphore(string, int, IDbTransaction) Constructor

Creates a semaphore with name [name](SqlDistributedSemaphore..ctor.h/TmHEv8QIhpjqtxBSqudA.md#Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string,int,System.Data.IDbTransaction).name 'Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string, int, System.Data.IDbTransaction).name') that can be acquired up to [maxCount](SqlDistributedSemaphore..ctor.h/TmHEv8QIhpjqtxBSqudA.md#Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string,int,System.Data.IDbTransaction).maxCount 'Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string, int, System.Data.IDbTransaction).maxCount')   
times concurrently. When acquired, the semaphore will be scoped to the given [transaction](SqlDistributedSemaphore..ctor.h/TmHEv8QIhpjqtxBSqudA.md#Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string,int,System.Data.IDbTransaction).transaction 'Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string, int, System.Data.IDbTransaction).transaction').   
The [transaction](SqlDistributedSemaphore..ctor.h/TmHEv8QIhpjqtxBSqudA.md#Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string,int,System.Data.IDbTransaction).transaction 'Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string, int, System.Data.IDbTransaction).transaction') and its [System.Data.IDbTransaction.Connection](https://docs.microsoft.com/en-us/dotnet/api/System.Data.IDbTransaction.Connection 'System.Data.IDbTransaction.Connection') are assumed to be externally managed:   
the [SqlDistributedSemaphore](SqlDistributedSemaphore.md 'Medallion.Threading.SqlServer.SqlDistributedSemaphore') will not attempt to open, close, commit, roll back, or dispose them

```csharp
public SqlDistributedSemaphore(string name, int maxCount, System.Data.IDbTransaction transaction);
```
#### Parameters

<a name='Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string,int,System.Data.IDbTransaction).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string,int,System.Data.IDbTransaction).maxCount'></a>

`maxCount` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')

<a name='Medallion.Threading.SqlServer.SqlDistributedSemaphore.SqlDistributedSemaphore(string,int,System.Data.IDbTransaction).transaction'></a>

`transaction` [System.Data.IDbTransaction](https://docs.microsoft.com/en-us/dotnet/api/System.Data.IDbTransaction 'System.Data.IDbTransaction')