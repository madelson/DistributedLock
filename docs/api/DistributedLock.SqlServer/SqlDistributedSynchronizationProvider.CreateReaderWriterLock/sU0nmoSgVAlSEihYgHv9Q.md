#### [DistributedLock.SqlServer](README.md 'README')
### [Medallion.Threading.SqlServer](Medallion.Threading.SqlServer.md 'Medallion.Threading.SqlServer').[SqlDistributedSynchronizationProvider](SqlDistributedSynchronizationProvider.md 'Medallion.Threading.SqlServer.SqlDistributedSynchronizationProvider')

## SqlDistributedSynchronizationProvider.CreateReaderWriterLock(string, bool) Method

Constructs an instance of [SqlDistributedReaderWriterLock](SqlDistributedReaderWriterLock.md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock') with the provided [name](SqlDistributedSynchronizationProvider.CreateReaderWriterLock./sU0nmoSgVAlSEihYgHv9Q.md#Medallion.Threading.SqlServer.SqlDistributedSynchronizationProvider.CreateReaderWriterLock(string,bool).name 'Medallion.Threading.SqlServer.SqlDistributedSynchronizationProvider.CreateReaderWriterLock(string, bool).name'). Unless [exactName](SqlDistributedSynchronizationProvider.CreateReaderWriterLock./sU0nmoSgVAlSEihYgHv9Q.md#Medallion.Threading.SqlServer.SqlDistributedSynchronizationProvider.CreateReaderWriterLock(string,bool).exactName 'Medallion.Threading.SqlServer.SqlDistributedSynchronizationProvider.CreateReaderWriterLock(string, bool).exactName') 
is specified, invalid applock names will be escaped/hashed.

```csharp
public Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock CreateReaderWriterLock(string name, bool exactName=false);
```
#### Parameters

<a name='Medallion.Threading.SqlServer.SqlDistributedSynchronizationProvider.CreateReaderWriterLock(string,bool).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.SqlServer.SqlDistributedSynchronizationProvider.CreateReaderWriterLock(string,bool).exactName'></a>

`exactName` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')

#### Returns
[SqlDistributedReaderWriterLock](SqlDistributedReaderWriterLock.md 'Medallion.Threading.SqlServer.SqlDistributedReaderWriterLock')