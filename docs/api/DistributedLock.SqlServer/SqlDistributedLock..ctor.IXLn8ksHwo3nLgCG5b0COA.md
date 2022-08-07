#### [DistributedLock.SqlServer](README.md 'README')
### [Medallion.Threading.SqlServer](Medallion.Threading.SqlServer.md 'Medallion.Threading.SqlServer').[SqlDistributedLock](SqlDistributedLock.md 'Medallion.Threading.SqlServer.SqlDistributedLock')

## SqlDistributedLock(string, string, Action<SqlConnectionOptionsBuilder>, bool) Constructor

Constructs a new lock using the provided [name](SqlDistributedLock..ctor.IXLn8ksHwo3nLgCG5b0COA.md#Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string,string,System.Action_Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder_,bool).name 'Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string, string, System.Action<Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder>, bool).name').   
  
The provided [connectionString](SqlDistributedLock..ctor.IXLn8ksHwo3nLgCG5b0COA.md#Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string,string,System.Action_Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder_,bool).connectionString 'Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string, string, System.Action<Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder>, bool).connectionString') will be used to connect to the database.  
  
Unless [exactName](SqlDistributedLock..ctor.IXLn8ksHwo3nLgCG5b0COA.md#Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string,string,System.Action_Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder_,bool).exactName 'Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string, string, System.Action<Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder>, bool).exactName') is specified, [name](SqlDistributedLock..ctor.IXLn8ksHwo3nLgCG5b0COA.md#Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string,string,System.Action_Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder_,bool).name 'Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string, string, System.Action<Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder>, bool).name') will be escaped/hashed to ensure name validity.

```csharp
public SqlDistributedLock(string name, string connectionString, System.Action<Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder>? options=null, bool exactName=false);
```
#### Parameters

<a name='Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string,string,System.Action_Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder_,bool).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string,string,System.Action_Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder_,bool).connectionString'></a>

`connectionString` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string,string,System.Action_Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder_,bool).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[SqlConnectionOptionsBuilder](SqlConnectionOptionsBuilder.md 'Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')

<a name='Medallion.Threading.SqlServer.SqlDistributedLock.SqlDistributedLock(string,string,System.Action_Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder_,bool).exactName'></a>

`exactName` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')