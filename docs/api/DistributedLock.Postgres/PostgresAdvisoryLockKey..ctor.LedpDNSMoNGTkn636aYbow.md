#### [DistributedLock.Postgres](README.md 'README')
### [Medallion.Threading.Postgres](Medallion.Threading.Postgres.md 'Medallion.Threading.Postgres').[PostgresAdvisoryLockKey](PostgresAdvisoryLockKey.md 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey')

## PostgresAdvisoryLockKey(int, int) Constructor

Constructs a key from a pair of 32-bit values. This is a separate key space 
than [PostgresAdvisoryLockKey(long)](PostgresAdvisoryLockKey..ctor.zWR+TwIbSGzvQzL9/uWeBQ.md 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey.PostgresAdvisoryLockKey(long)').

```csharp
public PostgresAdvisoryLockKey(int key1, int key2);
```
#### Parameters

<a name='Medallion.Threading.Postgres.PostgresAdvisoryLockKey.PostgresAdvisoryLockKey(int,int).key1'></a>

`key1` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')

<a name='Medallion.Threading.Postgres.PostgresAdvisoryLockKey.PostgresAdvisoryLockKey(int,int).key2'></a>

`key2` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')