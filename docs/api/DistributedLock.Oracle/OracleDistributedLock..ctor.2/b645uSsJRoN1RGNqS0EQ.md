#### [DistributedLock.Oracle](README.md 'README')
### [Medallion.Threading.Oracle](Medallion.Threading.Oracle.md 'Medallion.Threading.Oracle').[OracleDistributedLock](OracleDistributedLock.md 'Medallion.Threading.Oracle.OracleDistributedLock')

## OracleDistributedLock(string, string, Action<OracleConnectionOptionsBuilder>, bool) Constructor

Constructs a lock with the given [name](OracleDistributedLock..ctor.2/b645uSsJRoN1RGNqS0EQ.md#Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string,string,System.Action_Medallion.Threading.Oracle.OracleConnectionOptionsBuilder_,bool).name 'Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string, string, System.Action<Medallion.Threading.Oracle.OracleConnectionOptionsBuilder>, bool).name') that connects using the provided [connectionString](OracleDistributedLock..ctor.2/b645uSsJRoN1RGNqS0EQ.md#Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string,string,System.Action_Medallion.Threading.Oracle.OracleConnectionOptionsBuilder_,bool).connectionString 'Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string, string, System.Action<Medallion.Threading.Oracle.OracleConnectionOptionsBuilder>, bool).connectionString') and  
[options](OracleDistributedLock..ctor.2/b645uSsJRoN1RGNqS0EQ.md#Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string,string,System.Action_Medallion.Threading.Oracle.OracleConnectionOptionsBuilder_,bool).options 'Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string, string, System.Action<Medallion.Threading.Oracle.OracleConnectionOptionsBuilder>, bool).options').  
  
Unless [exactName](OracleDistributedLock..ctor.2/b645uSsJRoN1RGNqS0EQ.md#Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string,string,System.Action_Medallion.Threading.Oracle.OracleConnectionOptionsBuilder_,bool).exactName 'Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string, string, System.Action<Medallion.Threading.Oracle.OracleConnectionOptionsBuilder>, bool).exactName') is specified, [name](OracleDistributedLock..ctor.2/b645uSsJRoN1RGNqS0EQ.md#Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string,string,System.Action_Medallion.Threading.Oracle.OracleConnectionOptionsBuilder_,bool).name 'Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string, string, System.Action<Medallion.Threading.Oracle.OracleConnectionOptionsBuilder>, bool).name') will be escaped/hashed to ensure name validity.

```csharp
public OracleDistributedLock(string name, string connectionString, System.Action<Medallion.Threading.Oracle.OracleConnectionOptionsBuilder>? options=null, bool exactName=false);
```
#### Parameters

<a name='Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string,string,System.Action_Medallion.Threading.Oracle.OracleConnectionOptionsBuilder_,bool).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string,string,System.Action_Medallion.Threading.Oracle.OracleConnectionOptionsBuilder_,bool).connectionString'></a>

`connectionString` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string,string,System.Action_Medallion.Threading.Oracle.OracleConnectionOptionsBuilder_,bool).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[OracleConnectionOptionsBuilder](OracleConnectionOptionsBuilder.md 'Medallion.Threading.Oracle.OracleConnectionOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')

<a name='Medallion.Threading.Oracle.OracleDistributedLock.OracleDistributedLock(string,string,System.Action_Medallion.Threading.Oracle.OracleConnectionOptionsBuilder_,bool).exactName'></a>

`exactName` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')