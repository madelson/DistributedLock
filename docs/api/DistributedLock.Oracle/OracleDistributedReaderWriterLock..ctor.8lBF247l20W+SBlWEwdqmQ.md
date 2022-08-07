#### [DistributedLock.Oracle](README.md 'README')
### [Medallion.Threading.Oracle](Medallion.Threading.Oracle.md 'Medallion.Threading.Oracle').[OracleDistributedReaderWriterLock](OracleDistributedReaderWriterLock.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLock')

## OracleDistributedReaderWriterLock(string, string, Action<OracleConnectionOptionsBuilder>, bool) Constructor

Constructs a new lock using the provided [name](OracleDistributedReaderWriterLock..ctor.8lBF247l20W+SBlWEwdqmQ.md#Medallion.Threading.Oracle.OracleDistributedReaderWriterLock.OracleDistributedReaderWriterLock(string,string,System.Action_Medallion.Threading.Oracle.OracleConnectionOptionsBuilder_,bool).name 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLock.OracleDistributedReaderWriterLock(string, string, System.Action<Medallion.Threading.Oracle.OracleConnectionOptionsBuilder>, bool).name').   
  
The provided [connectionString](OracleDistributedReaderWriterLock..ctor.8lBF247l20W+SBlWEwdqmQ.md#Medallion.Threading.Oracle.OracleDistributedReaderWriterLock.OracleDistributedReaderWriterLock(string,string,System.Action_Medallion.Threading.Oracle.OracleConnectionOptionsBuilder_,bool).connectionString 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLock.OracleDistributedReaderWriterLock(string, string, System.Action<Medallion.Threading.Oracle.OracleConnectionOptionsBuilder>, bool).connectionString') will be used to connect to the database.  
  
Unless [exactName](OracleDistributedReaderWriterLock..ctor.8lBF247l20W+SBlWEwdqmQ.md#Medallion.Threading.Oracle.OracleDistributedReaderWriterLock.OracleDistributedReaderWriterLock(string,string,System.Action_Medallion.Threading.Oracle.OracleConnectionOptionsBuilder_,bool).exactName 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLock.OracleDistributedReaderWriterLock(string, string, System.Action<Medallion.Threading.Oracle.OracleConnectionOptionsBuilder>, bool).exactName') is specified, [name](OracleDistributedReaderWriterLock..ctor.8lBF247l20W+SBlWEwdqmQ.md#Medallion.Threading.Oracle.OracleDistributedReaderWriterLock.OracleDistributedReaderWriterLock(string,string,System.Action_Medallion.Threading.Oracle.OracleConnectionOptionsBuilder_,bool).name 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLock.OracleDistributedReaderWriterLock(string, string, System.Action<Medallion.Threading.Oracle.OracleConnectionOptionsBuilder>, bool).name') will be escaped/hashed to ensure name validity.

```csharp
public OracleDistributedReaderWriterLock(string name, string connectionString, System.Action<Medallion.Threading.Oracle.OracleConnectionOptionsBuilder>? options=null, bool exactName=false);
```
#### Parameters

<a name='Medallion.Threading.Oracle.OracleDistributedReaderWriterLock.OracleDistributedReaderWriterLock(string,string,System.Action_Medallion.Threading.Oracle.OracleConnectionOptionsBuilder_,bool).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.Oracle.OracleDistributedReaderWriterLock.OracleDistributedReaderWriterLock(string,string,System.Action_Medallion.Threading.Oracle.OracleConnectionOptionsBuilder_,bool).connectionString'></a>

`connectionString` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.Oracle.OracleDistributedReaderWriterLock.OracleDistributedReaderWriterLock(string,string,System.Action_Medallion.Threading.Oracle.OracleConnectionOptionsBuilder_,bool).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[OracleConnectionOptionsBuilder](OracleConnectionOptionsBuilder.md 'Medallion.Threading.Oracle.OracleConnectionOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')

<a name='Medallion.Threading.Oracle.OracleDistributedReaderWriterLock.OracleDistributedReaderWriterLock(string,string,System.Action_Medallion.Threading.Oracle.OracleConnectionOptionsBuilder_,bool).exactName'></a>

`exactName` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')