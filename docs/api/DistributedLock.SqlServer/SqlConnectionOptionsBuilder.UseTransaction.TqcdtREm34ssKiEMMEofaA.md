#### [DistributedLock.SqlServer](README.md 'README')
### [Medallion.Threading.SqlServer](Medallion.Threading.SqlServer.md 'Medallion.Threading.SqlServer').[SqlConnectionOptionsBuilder](SqlConnectionOptionsBuilder.md 'Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder')

## SqlConnectionOptionsBuilder.UseTransaction(bool) Method

Whether the synchronization should use a transaction scope rather than a session scope. Defaults to false.  
  
Synchronizing based on a transaction is marginally less expensive than using a connection  
because releasing requires only disposing the underlying [System.Data.IDbTransaction](https://docs.microsoft.com/en-us/dotnet/api/System.Data.IDbTransaction 'System.Data.IDbTransaction').  
The disadvantage is that using this strategy may lead to long-running transactions, which can be  
problematic for databases using the full recovery model.

```csharp
public Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder UseTransaction(bool useTransaction=true);
```
#### Parameters

<a name='Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder.UseTransaction(bool).useTransaction'></a>

`useTransaction` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')

#### Returns
[SqlConnectionOptionsBuilder](SqlConnectionOptionsBuilder.md 'Medallion.Threading.SqlServer.SqlConnectionOptionsBuilder')