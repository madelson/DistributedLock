#### [DistributedLock.SqlServer](README.md 'README')
### [Medallion.Threading.SqlServer](Medallion.Threading.SqlServer.md 'Medallion.Threading.SqlServer').[SqlDistributedReaderWriterLock](SqlDistributedReaderWriterLock.md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock')

## SqlDistributedReaderWriterLock(string, IDbTransaction, bool) Constructor

Constructs a new lock using the provided [name](SqlDistributedReaderWriterLock..ctor.V/fj2Yk0d/CnC3hDs1y3Cg.md#Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string,System.Data.IDbTransaction,bool).name 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string, System.Data.IDbTransaction, bool).name').  
  
The provided [transaction](SqlDistributedReaderWriterLock..ctor.V/fj2Yk0d/CnC3hDs1y3Cg.md#Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string,System.Data.IDbTransaction,bool).transaction 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string, System.Data.IDbTransaction, bool).transaction') will be used to connect to the database and will provide lock scope. It is assumed to be externally managed and  
will not be committed or rolled back.  
  
Unless [exactName](SqlDistributedReaderWriterLock..ctor.V/fj2Yk0d/CnC3hDs1y3Cg.md#Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string,System.Data.IDbTransaction,bool).exactName 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string, System.Data.IDbTransaction, bool).exactName') is specified, [name](SqlDistributedReaderWriterLock..ctor.V/fj2Yk0d/CnC3hDs1y3Cg.md#Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string,System.Data.IDbTransaction,bool).name 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string, System.Data.IDbTransaction, bool).name') will be escaped/hashed to ensure name validity.

```csharp
public SqlDistributedReaderWriterLock(string name, System.Data.IDbTransaction transaction, bool exactName=false);
```
#### Parameters

<a name='Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string,System.Data.IDbTransaction,bool).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string,System.Data.IDbTransaction,bool).transaction'></a>

`transaction` [System.Data.IDbTransaction](https://docs.microsoft.com/en-us/dotnet/api/System.Data.IDbTransaction 'System.Data.IDbTransaction')

<a name='Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock.SqlDistributedReaderWriterLock(string,System.Data.IDbTransaction,bool).exactName'></a>

`exactName` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')