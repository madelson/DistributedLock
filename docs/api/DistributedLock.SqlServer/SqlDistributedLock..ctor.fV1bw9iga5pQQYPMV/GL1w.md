#### [DistributedLock.SqlServer](README.md 'README')
### [Medallion.Threading.SqlServer](Medallion.Threading.SqlServer.md 'Medallion.Threading.SqlServer').[SqlDistributedLock](SqlDistributedLock.md 'Medallion.Threading.SqlServer.SqlDistributedLock')

## SqlDistributedLock(string, IDbTransaction, bool) Constructor

Constructs a new lock using the provided [name](SqlDistributedLock..ctor.fV1bw9iga5pQQYPMV/GL1w.md#Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string,System.Data.IDbTransaction,bool).name 'Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string, System.Data.IDbTransaction, bool).name').

The provided [transaction](SqlDistributedLock..ctor.fV1bw9iga5pQQYPMV/GL1w.md#Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string,System.Data.IDbTransaction,bool).transaction 'Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string, System.Data.IDbTransaction, bool).transaction') will be used to connect to the database and will provide lock scope. It is assumed to be externally managed and
will not be committed or rolled back.

Unless [exactName](SqlDistributedLock..ctor.fV1bw9iga5pQQYPMV/GL1w.md#Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string,System.Data.IDbTransaction,bool).exactName 'Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string, System.Data.IDbTransaction, bool).exactName') is specified, [name](SqlDistributedLock..ctor.fV1bw9iga5pQQYPMV/GL1w.md#Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string,System.Data.IDbTransaction,bool).name 'Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string, System.Data.IDbTransaction, bool).name') will be escaped/hashed to ensure name validity.

```csharp
public SqlDistributedLock(string name, System.Data.IDbTransaction transaction, bool exactName=false);
```
#### Parameters

<a name='Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string,System.Data.IDbTransaction,bool).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string,System.Data.IDbTransaction,bool).transaction'></a>

`transaction` [System.Data.IDbTransaction](https://docs.microsoft.com/en-us/dotnet/api/System.Data.IDbTransaction 'System.Data.IDbTransaction')

<a name='Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string,System.Data.IDbTransaction,bool).exactName'></a>

`exactName` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')