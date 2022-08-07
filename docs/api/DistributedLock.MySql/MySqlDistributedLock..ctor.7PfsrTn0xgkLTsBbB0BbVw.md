#### [DistributedLock.MySql](README.md 'README')
### [Medallion.Threading.MySql](Medallion.Threading.MySql.md 'Medallion.Threading.MySql').[MySqlDistributedLock](MySqlDistributedLock.md 'Medallion.Threading.MySql.MySqlDistributedLock')

## MySqlDistributedLock(string, IDbTransaction, bool) Constructor

Constructs a lock with the given [name](MySqlDistributedLock..ctor.7PfsrTn0xgkLTsBbB0BbVw.md#Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string,System.Data.IDbTransaction,bool).name 'Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string, System.Data.IDbTransaction, bool).name') that connects using the connection from the provided [transaction](MySqlDistributedLock..ctor.7PfsrTn0xgkLTsBbB0BbVw.md#Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string,System.Data.IDbTransaction,bool).transaction 'Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string, System.Data.IDbTransaction, bool).transaction').  
  
NOTE that the lock will not be scoped to the [transaction](MySqlDistributedLock..ctor.7PfsrTn0xgkLTsBbB0BbVw.md#Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string,System.Data.IDbTransaction,bool).transaction 'Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string, System.Data.IDbTransaction, bool).transaction') and must still be explicitly released before the transaction ends.  
However, this constructor allows the lock to PARTICIPATE in an ongoing transaction on a connection.  
  
Unless [exactName](MySqlDistributedLock..ctor.7PfsrTn0xgkLTsBbB0BbVw.md#Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string,System.Data.IDbTransaction,bool).exactName 'Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string, System.Data.IDbTransaction, bool).exactName') is specified, [name](MySqlDistributedLock..ctor.7PfsrTn0xgkLTsBbB0BbVw.md#Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string,System.Data.IDbTransaction,bool).name 'Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string, System.Data.IDbTransaction, bool).name') will be escaped/hashed to ensure name validity.

```csharp
public MySqlDistributedLock(string name, System.Data.IDbTransaction transaction, bool exactName=false);
```
#### Parameters

<a name='Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string,System.Data.IDbTransaction,bool).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string,System.Data.IDbTransaction,bool).transaction'></a>

`transaction` [System.Data.IDbTransaction](https://docs.microsoft.com/en-us/dotnet/api/System.Data.IDbTransaction 'System.Data.IDbTransaction')

<a name='Medallion.Threading.MySql.MySqlDistributedLock.MySqlDistributedLock(string,System.Data.IDbTransaction,bool).exactName'></a>

`exactName` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')