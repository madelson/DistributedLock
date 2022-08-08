#### [DistributedLock.SqlServer](README.md 'README')
### [Medallion.Threading.SqlServer](Medallion.Threading.SqlServer.md 'Medallion.Threading.SqlServer').[SqlDistributedReaderWriterLock](SqlDistributedReaderWriterLock.md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock')

## SqlDistributedReaderWriterLock(string, IDbConnection, bool) Constructor

Constructs a new lock using the provided [name](SqlDistributedReaderWriterLock..ctor.aQ4wmiPxon1R/D2jE25+dg.md#Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string,System.Data.IDbConnection,bool).name 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string, System.Data.IDbConnection, bool).name').

The provided [connection](SqlDistributedReaderWriterLock..ctor.aQ4wmiPxon1R/D2jE25+dg.md#Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string,System.Data.IDbConnection,bool).connection 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string, System.Data.IDbConnection, bool).connection') will be used to connect to the database and will provide lock scope. It is assumed to be externally managed and
will not be opened or closed.

Unless [exactName](SqlDistributedReaderWriterLock..ctor.aQ4wmiPxon1R/D2jE25+dg.md#Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string,System.Data.IDbConnection,bool).exactName 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string, System.Data.IDbConnection, bool).exactName') is specified, [name](SqlDistributedReaderWriterLock..ctor.aQ4wmiPxon1R/D2jE25+dg.md#Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string,System.Data.IDbConnection,bool).name 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string, System.Data.IDbConnection, bool).name') will be escaped/hashed to ensure name validity.

```csharp
public SqlDistributedReaderWriterLock(string name, System.Data.IDbConnection connection, bool exactName=false);
```
#### Parameters

<a name='Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string,System.Data.IDbConnection,bool).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string,System.Data.IDbConnection,bool).connection'></a>

`connection` [System.Data.IDbConnection](https://docs.microsoft.com/en-us/dotnet/api/System.Data.IDbConnection 'System.Data.IDbConnection')

<a name='Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string,System.Data.IDbConnection,bool).exactName'></a>

`exactName` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')