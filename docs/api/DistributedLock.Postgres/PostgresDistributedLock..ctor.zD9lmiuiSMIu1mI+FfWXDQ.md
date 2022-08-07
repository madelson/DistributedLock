#### [DistributedLock.Postgres](README.md 'README')
### [Medallion.Threading.Postgres](Medallion.Threading.Postgres.md 'Medallion.Threading.Postgres').[PostgresDistributedLock](PostgresDistributedLock.md 'Medallion.Threading.Postgres.PostgresDistributedLock')

## PostgresDistributedLock(PostgresAdvisoryLockKey, string, Action<PostgresConnectionOptionsBuilder>) Constructor

Constructs a lock with the given [key](PostgresDistributedLock..ctor.zD9lmiuiSMIu1mI+FfWXDQ.md#Medallion.Threading.Postgres.PostgresDistributedLock.PostgresDistributedLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey,string,System.Action_Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder_).key 'Medallion.Threading.Postgres.PostgresDistributedLock.PostgresDistributedLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey, string, System.Action<Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder>).key') (effectively the lock name), [connectionString](PostgresDistributedLock..ctor.zD9lmiuiSMIu1mI+FfWXDQ.md#Medallion.Threading.Postgres.PostgresDistributedLock.PostgresDistributedLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey,string,System.Action_Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder_).connectionString 'Medallion.Threading.Postgres.PostgresDistributedLock.PostgresDistributedLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey, string, System.Action<Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder>).connectionString'),  
and [options](PostgresDistributedLock..ctor.zD9lmiuiSMIu1mI+FfWXDQ.md#Medallion.Threading.Postgres.PostgresDistributedLock.PostgresDistributedLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey,string,System.Action_Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder_).options 'Medallion.Threading.Postgres.PostgresDistributedLock.PostgresDistributedLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey, string, System.Action<Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder>).options')

```csharp
public PostgresDistributedLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey key, string connectionString, System.Action<Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder>? options=null);
```
#### Parameters

<a name='Medallion.Threading.Postgres.PostgresDistributedLock.PostgresDistributedLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey,string,System.Action_Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder_).key'></a>

`key` [PostgresAdvisoryLockKey](PostgresAdvisoryLockKey.md 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey')

<a name='Medallion.Threading.Postgres.PostgresDistributedLock.PostgresDistributedLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey,string,System.Action_Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder_).connectionString'></a>

`connectionString` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.Postgres.PostgresDistributedLock.PostgresDistributedLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey,string,System.Action_Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder_).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[PostgresConnectionOptionsBuilder](PostgresConnectionOptionsBuilder.md 'Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')