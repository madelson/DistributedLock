#### [DistributedLock.Oracle](README.md 'README')
### [Medallion.Threading.Oracle](Medallion.Threading.Oracle.md 'Medallion.Threading.Oracle').[OracleDistributedLock](OracleDistributedLock.md 'Medallion.Threading.Oracle.OracleDistributedLock')

## OracleDistributedLock(string, IDbConnection, bool) Constructor

Constructs a lock with the given [name](OracleDistributedLock..ctor.ruFnlqTt8A/yRSLZE0t3dA.md#Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string,System.Data.IDbConnection,bool).name 'Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string, System.Data.IDbConnection, bool).name') that connects using the provided [connection](OracleDistributedLock..ctor.ruFnlqTt8A/yRSLZE0t3dA.md#Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string,System.Data.IDbConnection,bool).connection 'Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string, System.Data.IDbConnection, bool).connection').

Unless [exactName](OracleDistributedLock..ctor.ruFnlqTt8A/yRSLZE0t3dA.md#Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string,System.Data.IDbConnection,bool).exactName 'Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string, System.Data.IDbConnection, bool).exactName') is specified, [name](OracleDistributedLock..ctor.ruFnlqTt8A/yRSLZE0t3dA.md#Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string,System.Data.IDbConnection,bool).name 'Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string, System.Data.IDbConnection, bool).name') will be escaped/hashed to ensure name validity.

```csharp
public OracleDistributedLock(string name, System.Data.IDbConnection connection, bool exactName=false);
```
#### Parameters

<a name='Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string,System.Data.IDbConnection,bool).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string,System.Data.IDbConnection,bool).connection'></a>

`connection` [System.Data.IDbConnection](https://docs.microsoft.com/en-us/dotnet/api/System.Data.IDbConnection 'System.Data.IDbConnection')

<a name='Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string,System.Data.IDbConnection,bool).exactName'></a>

`exactName` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')