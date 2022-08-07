#### [DistributedLock.SqlServer](README.md 'README')
### [Medallion.Threading.SqlServer](Medallion.Threading.SqlServer.md 'Medallion.Threading.SqlServer').[SqlDistributedLock](SqlDistributedLock.md 'Medallion.Threading.SqlServer.SqlDistributedLock')

## SqlDistributedLock(string, IDbConnection, bool) Constructor

Constructs a new lock using the provided [name](SqlDistributedLock..ctor.OZ2lCJsURZ+h8HKzPvymjw.md#Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string,System.Data.IDbConnection,bool).name 'Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string, System.Data.IDbConnection, bool).name').  
  
The provided [connection](SqlDistributedLock..ctor.OZ2lCJsURZ+h8HKzPvymjw.md#Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string,System.Data.IDbConnection,bool).connection 'Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string, System.Data.IDbConnection, bool).connection') will be used to connect to the database and will provide lock scope. It is assumed to be externally managed and  
will not be opened or closed.  
  
Unless [exactName](SqlDistributedLock..ctor.OZ2lCJsURZ+h8HKzPvymjw.md#Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string,System.Data.IDbConnection,bool).exactName 'Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string, System.Data.IDbConnection, bool).exactName') is specified, [name](SqlDistributedLock..ctor.OZ2lCJsURZ+h8HKzPvymjw.md#Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string,System.Data.IDbConnection,bool).name 'Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string, System.Data.IDbConnection, bool).name') will be escaped/hashed to ensure name validity.

```csharp
public SqlDistributedLock(string name, System.Data.IDbConnection connection, bool exactName=false);
```
#### Parameters

<a name='Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string,System.Data.IDbConnection,bool).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string,System.Data.IDbConnection,bool).connection'></a>

`connection` [System.Data.IDbConnection](https://docs.microsoft.com/en-us/dotnet/api/System.Data.IDbConnection 'System.Data.IDbConnection')

<a name='Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string,System.Data.IDbConnection,bool).exactName'></a>

`exactName` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')