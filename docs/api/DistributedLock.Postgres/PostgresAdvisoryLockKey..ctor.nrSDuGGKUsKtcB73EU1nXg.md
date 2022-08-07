#### [DistributedLock.Postgres](README.md 'README')
### [Medallion.Threading.Postgres](Medallion.Threading.Postgres.md 'Medallion.Threading.Postgres').[PostgresAdvisoryLockKey](PostgresAdvisoryLockKey.md 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey')

## PostgresAdvisoryLockKey(string, bool) Constructor

Constructs a key based on a string [name](PostgresAdvisoryLockKey..ctor.nrSDuGGKUsKtcB73EU1nXg.md#Medallion.Threading.Postgres.PostgresAdvisoryLockKey.PostgresAdvisoryLockKey(string,bool).name 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey.PostgresAdvisoryLockKey(string, bool).name').  
  
If the string is of the form "{16-digit hex}" or "{8-digit hex},{8-digit hex}", this will be parsed into numeric keys.  
  
If the string is an ascii string with 9 or fewer characters, it will be mapped to a key that does not collide with  
any other key based on such a string or based on a 32-bit value.  
  
Other string names will be rejected unless [allowHashing](PostgresAdvisoryLockKey..ctor.nrSDuGGKUsKtcB73EU1nXg.md#Medallion.Threading.Postgres.PostgresAdvisoryLockKey.PostgresAdvisoryLockKey(string,bool).allowHashing 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey.PostgresAdvisoryLockKey(string, bool).allowHashing') is specified, in which case it will be hashed to  
a 64-bit key value.

```csharp
public PostgresAdvisoryLockKey(string name, bool allowHashing=false);
```
#### Parameters

<a name='Medallion.Threading.Postgres.PostgresAdvisoryLockKey.PostgresAdvisoryLockKey(string,bool).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.Postgres.PostgresAdvisoryLockKey.PostgresAdvisoryLockKey(string,bool).allowHashing'></a>

`allowHashing` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')