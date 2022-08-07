#### [DistributedLock.Oracle](README.md 'README')
### [Medallion.Threading.Oracle](Medallion.Threading.Oracle.md 'Medallion.Threading.Oracle').[OracleDistributedSynchronizationProvider](OracleDistributedSynchronizationProvider.md 'Medallion.Threading.Oracle.OracleDistributedSynchronizationProvider')

## OracleDistributedSynchronizationProvider.CreateReaderWriterLock(string, bool) Method

Creates a [OracleDistributedReaderWriterLock](OracleDistributedReaderWriterLock.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLock') with the provided [name](OracleDistributedSynchronizationProvider.CreateReaderWriterLock.c5P1XG9aduRCEZMRz2ynkQ.md#Medallion.Threading.Oracle.OracleDistributedSynchronizationProvider.CreateReaderWriterLock(string,bool).name 'Medallion.Threading.Oracle.OracleDistributedSynchronizationProvider.CreateReaderWriterLock(string, bool).name'). Unless [exactName](OracleDistributedSynchronizationProvider.CreateReaderWriterLock.c5P1XG9aduRCEZMRz2ynkQ.md#Medallion.Threading.Oracle.OracleDistributedSynchronizationProvider.CreateReaderWriterLock(string,bool).exactName 'Medallion.Threading.Oracle.OracleDistributedSynchronizationProvider.CreateReaderWriterLock(string, bool).exactName')   
is specified, invalid names will be escaped/hashed.

```csharp
public Medallion.Threading.Oracle.OracleDistributedReaderWriterLock CreateReaderWriterLock(string name, bool exactName=false);
```
#### Parameters

<a name='Medallion.Threading.Oracle.OracleDistributedSynchronizationProvider.CreateReaderWriterLock(string,bool).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.Oracle.OracleDistributedSynchronizationProvider.CreateReaderWriterLock(string,bool).exactName'></a>

`exactName` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')

#### Returns
[OracleDistributedReaderWriterLock](OracleDistributedReaderWriterLock.md 'Medallion.Threading.Oracle.OracleDistributedReaderWriterLock')