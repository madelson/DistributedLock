#### [DistributedLock.MySql](README.md 'README')
### [Medallion.Threading.MySql](Medallion.Threading.MySql.md 'Medallion.Threading.MySql').[MySqlDistributedLock](MySqlDistributedLock.md 'Medallion.Threading.MySql.MySqlDistributedLock')

## MySqlDistributedLock(string, IDbConnection, bool) Constructor

Constructs a lock with the given [name](MySqlDistributedLock..ctor.Gw0JTgvDod2OkXGtyloMgg.md#Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string,System.Data.IDbConnection,bool).name 'Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string, System.Data.IDbConnection, bool).name') that connects using the provided [connection](MySqlDistributedLock..ctor.Gw0JTgvDod2OkXGtyloMgg.md#Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string,System.Data.IDbConnection,bool).connection 'Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string, System.Data.IDbConnection, bool).connection').

Unless [exactName](MySqlDistributedLock..ctor.Gw0JTgvDod2OkXGtyloMgg.md#Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string,System.Data.IDbConnection,bool).exactName 'Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string, System.Data.IDbConnection, bool).exactName') is specified, [name](MySqlDistributedLock..ctor.Gw0JTgvDod2OkXGtyloMgg.md#Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string,System.Data.IDbConnection,bool).name 'Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string, System.Data.IDbConnection, bool).name') will be escaped/hashed to ensure name validity.

```csharp
public MySqlDistributedLock(string name, System.Data.IDbConnection connection, bool exactName=false);
```
#### Parameters

<a name='Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string,System.Data.IDbConnection,bool).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string,System.Data.IDbConnection,bool).connection'></a>

`connection` [System.Data.IDbConnection](https://docs.microsoft.com/en-us/dotnet/api/System.Data.IDbConnection 'System.Data.IDbConnection')

<a name='Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string,System.Data.IDbConnection,bool).exactName'></a>

`exactName` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')