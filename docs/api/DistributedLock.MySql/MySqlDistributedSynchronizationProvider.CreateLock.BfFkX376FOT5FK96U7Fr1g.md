#### [DistributedLock.MySql](README.md 'README')
### [Medallion.Threading.MySql](Medallion.Threading.MySql.md 'Medallion.Threading.MySql').[MySqlDistributedSynchronizationProvider](MySqlDistributedSynchronizationProvider.md 'Medallion.Threading.MySql.MySqlDistributedSynchronizationProvider')

## MySqlDistributedSynchronizationProvider.CreateLock(string, bool) Method

Creates a [MySqlDistributedLock](MySqlDistributedLock.md 'Medallion.Threading.MySql.MySqlDistributedLock') with the provided [name](MySqlDistributedSynchronizationProvider.CreateLock.BfFkX376FOT5FK96U7Fr1g.md#Medallion.Threading.MySql.MySqlDistributedSynchronizationProvider.CreateLock(string,bool).name 'Medallion.Threading.MySql.MySqlDistributedSynchronizationProvider.CreateLock(string, bool).name'). Unless [exactName](MySqlDistributedSynchronizationProvider.CreateLock.BfFkX376FOT5FK96U7Fr1g.md#Medallion.Threading.MySql.MySqlDistributedSynchronizationProvider.CreateLock(string,bool).exactName 'Medallion.Threading.MySql.MySqlDistributedSynchronizationProvider.CreateLock(string, bool).exactName')   
is specified, invalid names will be escaped/hashed.

```csharp
public Medallion.Threading.MySql.MySqlDistributedLock CreateLock(string name, bool exactName=false);
```
#### Parameters

<a name='Medallion.Threading.MySql.MySqlDistributedSynchronizationProvider.CreateLock(string,bool).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.MySql.MySqlDistributedSynchronizationProvider.CreateLock(string,bool).exactName'></a>

`exactName` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')

#### Returns
[MySqlDistributedLock](MySqlDistributedLock.md 'Medallion.Threading.MySql.MySqlDistributedLock')