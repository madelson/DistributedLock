#### [DistributedLock.Postgres](README.md 'README')
### [Medallion.Threading.Postgres](Medallion.Threading.Postgres.md 'Medallion.Threading.Postgres')

## PostgresDistributedLock Class

Implements a distributed lock using Postgres advisory locks  
(see https://www.postgresql.org/docs/12/functions-admin.html#FUNCTIONS-ADVISORY-LOCKS)

```csharp
public sealed class PostgresDistributedLock :
Medallion.Threading.IDistributedLock
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; PostgresDistributedLock

Implements [IDistributedLock](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLock.md 'Medallion.Threading.IDistributedLock')

| Constructors | |
| :--- | :--- |
| [PostgresDistributedLock(PostgresAdvisoryLockKey, string, Action&lt;PostgresConnectionOptionsBuilder&gt;)](PostgresDistributedLock..ctor.zD9lmiuiSMIu1mI+FfWXDQ.md 'Medallion.Threading.Postgres.PostgresDistributedLock.PostgresDistributedLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey, string, System.Action<Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder>)') | Constructs a lock with the given [key](PostgresDistributedLock..ctor.zD9lmiuiSMIu1mI+FfWXDQ.md#Medallion.Threading.Postgres.PostgresDistributedLock.PostgresDistributedLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey,string,System.Action_Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder_).key 'Medallion.Threading.Postgres.PostgresDistributedLock.PostgresDistributedLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey, string, System.Action<Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder>).key') (effectively the lock name), [connectionString](PostgresDistributedLock..ctor.zD9lmiuiSMIu1mI+FfWXDQ.md#Medallion.Threading.Postgres.PostgresDistributedLock.PostgresDistributedLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey,string,System.Action_Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder_).connectionString 'Medallion.Threading.Postgres.PostgresDistributedLock.PostgresDistributedLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey, string, System.Action<Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder>).connectionString'),<br/>and [options](PostgresDistributedLock..ctor.zD9lmiuiSMIu1mI+FfWXDQ.md#Medallion.Threading.Postgres.PostgresDistributedLock.PostgresDistributedLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey,string,System.Action_Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder_).options 'Medallion.Threading.Postgres.PostgresDistributedLock.PostgresDistributedLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey, string, System.Action<Medallion.Threading.Postgres.PostgresConnectionOptionsBuilder>).options') |
| [PostgresDistributedLock(PostgresAdvisoryLockKey, IDbConnection)](PostgresDistributedLock..ctor.WrNsj1JRkAGtaHLdHnRFrg.md 'Medallion.Threading.Postgres.PostgresDistributedLock.PostgresDistributedLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey, System.Data.IDbConnection)') | Constructs a lock with the given [key](PostgresDistributedLock..ctor.WrNsj1JRkAGtaHLdHnRFrg.md#Medallion.Threading.Postgres.PostgresDistributedLock.PostgresDistributedLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey,System.Data.IDbConnection).key 'Medallion.Threading.Postgres.PostgresDistributedLock.PostgresDistributedLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey, System.Data.IDbConnection).key') (effectively the lock name) and [connection](PostgresDistributedLock..ctor.WrNsj1JRkAGtaHLdHnRFrg.md#Medallion.Threading.Postgres.PostgresDistributedLock.PostgresDistributedLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey,System.Data.IDbConnection).connection 'Medallion.Threading.Postgres.PostgresDistributedLock.PostgresDistributedLock(Medallion.Threading.Postgres.PostgresAdvisoryLockKey, System.Data.IDbConnection).connection'). |

| Properties | |
| :--- | :--- |
| [Key](PostgresDistributedLock.Key.md 'Medallion.Threading.Postgres.PostgresDistributedLock.Key') | The [PostgresAdvisoryLockKey](PostgresAdvisoryLockKey.md 'Medallion.Threading.Postgres.PostgresAdvisoryLockKey') that uniquely identifies the lock on the database |

| Methods | |
| :--- | :--- |
| [Acquire(Nullable&lt;TimeSpan&gt;, CancellationToken)](PostgresDistributedLock.Acquire.wevmsPrIfDcimdIlhBziiw.md 'Medallion.Threading.Postgres.PostgresDistributedLock.Acquire(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Acquires the lock synchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Usage: |
| [AcquireAsync(Nullable&lt;TimeSpan&gt;, CancellationToken)](PostgresDistributedLock.AcquireAsync.Vmz1wx/1suZB7AWMDkEWqA.md 'Medallion.Threading.Postgres.PostgresDistributedLock.AcquireAsync(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Acquires the lock asynchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Usage: |
| [TryAcquire(TimeSpan, CancellationToken)](PostgresDistributedLock.TryAcquire.m+KJTn2pfMV+kQiMfrpLuQ.md 'Medallion.Threading.Postgres.PostgresDistributedLock.TryAcquire(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to acquire the lock synchronously. Usage: |
| [TryAcquireAsync(TimeSpan, CancellationToken)](PostgresDistributedLock.TryAcquireAsync.vpSo9yMiuK1NYo8xC2ACUw.md 'Medallion.Threading.Postgres.PostgresDistributedLock.TryAcquireAsync(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to acquire the lock asynchronously. Usage: |
