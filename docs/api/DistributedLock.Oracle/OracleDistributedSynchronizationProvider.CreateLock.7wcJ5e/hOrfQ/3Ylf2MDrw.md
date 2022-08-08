#### [DistributedLock.Oracle](README.md 'README')
### [Medallion.Threading.Oracle](Medallion.Threading.Oracle.md 'Medallion.Threading.Oracle').[OracleDistributedSynchronizationProvider](OracleDistributedSynchronizationProvider.md 'Medallion.Threading.Oracle.OracleDistributedSynchronizationProvider')

## OracleDistributedSynchronizationProvider.CreateLock(string, bool) Method

Creates a [OracleDistributedLock](OracleDistributedLock.md 'Medallion.Threading.Oracle.OracleDistributedLock') with the provided [name](OracleDistributedSynchronizationProvider.CreateLock.7wcJ5e/hOrfQ/3Ylf2MDrw.md#Medallion.Threading.Oracle.OracleDistributedSynchronizationProvider.CreateLock(string,bool).name 'Medallion.Threading.Oracle.OracleDistributedSynchronizationProvider.CreateLock(string, bool).name'). Unless [exactName](OracleDistributedSynchronizationProvider.CreateLock.7wcJ5e/hOrfQ/3Ylf2MDrw.md#Medallion.Threading.Oracle.OracleDistributedSynchronizationProvider.CreateLock(string,bool).exactName 'Medallion.Threading.Oracle.OracleDistributedSynchronizationProvider.CreateLock(string, bool).exactName') 
is specified, invalid names will be escaped/hashed.

```csharp
public Medallion.Threading.Oracle.OracleDistributedLock CreateLock(string name, bool exactName=false);
```
#### Parameters

<a name='Medallion.Threading.Oracle.OracleDistributedSynchronizationProvider.CreateLock(string,bool).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.Oracle.OracleDistributedSynchronizationProvider.CreateLock(string,bool).exactName'></a>

`exactName` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')

#### Returns
[OracleDistributedLock](OracleDistributedLock.md 'Medallion.Threading.Oracle.OracleDistributedLock')