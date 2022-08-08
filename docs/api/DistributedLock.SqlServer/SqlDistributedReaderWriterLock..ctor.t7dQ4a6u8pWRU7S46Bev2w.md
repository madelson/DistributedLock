#### [DistributedLock.SqlServer](README.md 'README')
### [Medallion.Threading.SqlServer](Medallion.Threading.SqlServer.md 'Medallion.Threading.SqlServer').[SqlDistributedReaderWriterLock](SqlDistributedReaderWriterLock.md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock')

## SqlDistributedReaderWriterLock(string, string, Action<SqlConnectionOptionsBuilder>, bool) Constructor

Constructs a new lock using the provided [name](SqlDistributedReaderWriterLock..ctor.t7dQ4a6u8pWRU7S46Bev2w.md#Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string,string,System.Action_Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder_,bool).name 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string, string, System.Action<Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder>, bool).name'). 

The provided [connectionString](SqlDistributedReaderWriterLock..ctor.t7dQ4a6u8pWRU7S46Bev2w.md#Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string,string,System.Action_Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder_,bool).connectionString 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string, string, System.Action<Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder>, bool).connectionString') will be used to connect to the database.

Unless [exactName](SqlDistributedReaderWriterLock..ctor.t7dQ4a6u8pWRU7S46Bev2w.md#Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string,string,System.Action_Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder_,bool).exactName 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string, string, System.Action<Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder>, bool).exactName') is specified, [name](SqlDistributedReaderWriterLock..ctor.t7dQ4a6u8pWRU7S46Bev2w.md#Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string,string,System.Action_Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder_,bool).name 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string, string, System.Action<Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder>, bool).name') will be escaped/hashed to ensure name validity.

```csharp
public SqlDistributedReaderWriterLock(string name, string connectionString, System.Action<Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder>? options=null, bool exactName=false);
```
#### Parameters

<a name='Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string,string,System.Action_Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder_,bool).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string,string,System.Action_Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder_,bool).connectionString'></a>

`connectionString` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string,string,System.Action_Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder_,bool).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[SqlConnectionOptionsBuilder](SqlConnectionOptionsBuilder.md 'Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')

<a name='Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string,string,System.Action_Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder_,bool).exactName'></a>

`exactName` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')