#### [DistributedLock.SqlServer](README.md 'README')
### [Medallion.Threading.SqlServer](Medallion.Threading.SqlServer.md 'Medallion.Threading.SqlServer').[SqlDistributedSynchronizationProvider](SqlDistributedSynchronizationProvider.md 'Medallion.Threading.SqlServer.SqlDistributedSynchronizationProvider')

## SqlDistributedSynchronizationProvider.CreateLock(string, bool) Method

Constructs an instance of [SqlDistributedLock](SqlDistributedLock.md 'Medallion.Threading.SqlServer.SqlDistributedLock') with the provided [name](SqlDistributedSynchronizationProvider.CreateLock.OBB/tAHXb7DSNADefkuRKg.md#Medallion.Threading.SqlServer.SqlDistributedSynchronizationProvider.CreateLock(string,bool).name 'Medallion.Threading.SqlServer.SqlDistributedSynchronizationProvider.CreateLock(string, bool).name'). Unless [exactName](SqlDistributedSynchronizationProvider.CreateLock.OBB/tAHXb7DSNADefkuRKg.md#Medallion.Threading.SqlServer.SqlDistributedSynchronizationProvider.CreateLock(string,bool).exactName 'Medallion.Threading.SqlServer.SqlDistributedSynchronizationProvider.CreateLock(string, bool).exactName') 
is specified, invalid applock names will be escaped/hashed.

```csharp
public Medallion.Threading.SqlServer.SqlDistributedLock CreateLock(string name, bool exactName=false);
```
#### Parameters

<a name='Medallion.Threading.SqlServer.SqlDistributedSynchronizationProvider.CreateLock(string,bool).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.SqlServer.SqlDistributedSynchronizationProvider.CreateLock(string,bool).exactName'></a>

`exactName` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')

#### Returns
[SqlDistributedLock](SqlDistributedLock.md 'Medallion.Threading.SqlServer.SqlDistributedLock')