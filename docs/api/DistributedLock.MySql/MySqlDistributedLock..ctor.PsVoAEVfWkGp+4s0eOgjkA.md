#### [DistributedLock.MySql](README.md 'README')
### [Medallion.Threading.MySql](Medallion.Threading.MySql.md 'Medallion.Threading.MySql').[MySqlDistributedLock](MySqlDistributedLock.md 'Medallion.Threading.MySql.MySqlDistributedLock')

## MySqlDistributedLock(string, string, Action<MySqlConnectionOptionsBuilder>, bool) Constructor

Constructs a lock with the given [name](MySqlDistributedLock..ctor.PsVoAEVfWkGp+4s0eOgjkA.md#Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string,string,System.Action_Medallion.Threading.MySql.MySqlConnectionOptionsBuilder_,bool).name 'Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string, string, System.Action<Medallion.Threading.MySql.MySqlConnectionOptionsBuilder>, bool).name') that connects using the provided [connectionString](MySqlDistributedLock..ctor.PsVoAEVfWkGp+4s0eOgjkA.md#Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string,string,System.Action_Medallion.Threading.MySql.MySqlConnectionOptionsBuilder_,bool).connectionString 'Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string, string, System.Action<Medallion.Threading.MySql.MySqlConnectionOptionsBuilder>, bool).connectionString') and
[options](MySqlDistributedLock..ctor.PsVoAEVfWkGp+4s0eOgjkA.md#Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string,string,System.Action_Medallion.Threading.MySql.MySqlConnectionOptionsBuilder_,bool).options 'Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string, string, System.Action<Medallion.Threading.MySql.MySqlConnectionOptionsBuilder>, bool).options').

Unless [exactName](MySqlDistributedLock..ctor.PsVoAEVfWkGp+4s0eOgjkA.md#Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string,string,System.Action_Medallion.Threading.MySql.MySqlConnectionOptionsBuilder_,bool).exactName 'Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string, string, System.Action<Medallion.Threading.MySql.MySqlConnectionOptionsBuilder>, bool).exactName') is specified, [name](MySqlDistributedLock..ctor.PsVoAEVfWkGp+4s0eOgjkA.md#Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string,string,System.Action_Medallion.Threading.MySql.MySqlConnectionOptionsBuilder_,bool).name 'Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string, string, System.Action<Medallion.Threading.MySql.MySqlConnectionOptionsBuilder>, bool).name') will be escaped/hashed to ensure name validity.

```csharp
public MySqlDistributedLock(string name, string connectionString, System.Action<Medallion.Threading.MySql.MySqlConnectionOptionsBuilder>? options=null, bool exactName=false);
```
#### Parameters

<a name='Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string,string,System.Action_Medallion.Threading.MySql.MySqlConnectionOptionsBuilder_,bool).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string,string,System.Action_Medallion.Threading.MySql.MySqlConnectionOptionsBuilder_,bool).connectionString'></a>

`connectionString` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string,string,System.Action_Medallion.Threading.MySql.MySqlConnectionOptionsBuilder_,bool).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[MySqlConnectionOptionsBuilder](MySqlConnectionOptionsBuilder.md 'Medallion.Threading.MySql.MySqlConnectionOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')

<a name='Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string,string,System.Action_Medallion.Threading.MySql.MySqlConnectionOptionsBuilder_,bool).exactName'></a>

`exactName` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')