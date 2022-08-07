#### [DistributedLock.Postgres](README.md 'README')
### [Medallion.Threading.Postgres](Medallion.Threading.Postgres.md 'Medallion.Threading.Postgres').[PostgresDistributedReaderWriterLock](PostgresDistributedReaderWriterLock.md 'Medallion.Threading.Postgres.PostgresDistributedReaderWriterLock')

## PostgresDistributedReaderWriterLock(PostgresAdvisoryLockKey, IDbConnection) Constructor

Constructs a lock with the given [key](PostgresDistributedReaderWriterLock..ctor.W6WkSdoxDDf2G56xXhWE6A.md#Medallion.Threading.Postgres.PostgresDistributedReaderWriterLock.PostgresDistributedReaderWriterLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey,System.Data.IDbConnection).key 'Medallion.Threading.Postgres.PostgresDistributedReaderWriterLock.PostgresDistributedReaderWriterLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey, System.Data.IDbConnection).key') (effectively the lock name) and [connection](PostgresDistributedReaderWriterLock..ctor.W6WkSdoxDDf2G56xXhWE6A.md#Medallion.Threading.Postgres.PostgresDistributedReaderWriterLock.PostgresDistributedReaderWriterLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey,System.Data.IDbConnection).connection 'Medallion.Threading.Postgres.PostgresDistributedReaderWriterLock.PostgresDistributedReaderWriterLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey, System.Data.IDbConnection).connection').

```csharp
public PostgresDistributedReaderWriterLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey key, System.Data.IDbConnection connection);
```
#### Parameters

<a name='Medallion.Threading.Postgres.PostgresDistributedReaderWriterLock.PostgresDistributedReaderWriterLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey,System.Data.IDbConnection).key'></a>

`key` [PostgresAdvisoryLockKey](PostgresAdvisoryLockKey.md 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey')

<a name='Medallion.Threading.Postgres.PostgresDistributedReaderWriterLock.PostgresDistributedReaderWriterLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey,System.Data.IDbConnection).connection'></a>

`connection` [System.Data.IDbConnection](https://docs.microsoft.com/en-us/dotnet/api/System.Data.IDbConnection 'System.Data.IDbConnection')