# DistributedLock.SqlServer

[Download the NuGet package](https://www.nuget.org/packages/DistributedLock.SqlServer) [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.SqlServer.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.SqlServer/)

The DistributedLock.SqlServer package offers distributed locks based on [Microsoft SQL Server application locks](https://docs.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-getapplock-transact-sql?view=sql-server-ver15). For example:

```C#
var @lock = new SqlDistributedLock("MyLockName", connectionString);
await using (await @lock.AcquireAsync())
{
  // I have the lock
}
```

## APIs

- The `SqlDistributedLock` class implements the `IDistributedLock` interface.
- The `SqlDistributedReaderWriterLock` class implements the `IDistributedUpgradeableReaderWriterLock` interface.
- The `SqlDistributedSemaphore` class implements the `IDistributedSemaphore` interface.
- The `SqlDistributedSynchronizationProvider` class implements the `IDistributedLockProvider`, `IDistributedUpgradeableReaderWriterLockProvider`, and `IDistributedSemaphoreProvider` interfaces.

## Implementation notes

SQL-based locks can be constructed with a connection string, an `IDbConnection`, or an `IDbTransaction`. When a connection is passed, the lock will be scoped to that connection and when a transaction is passed the lock will be scoped to that transaction. In most cases, using a `connectionString` is preferred because it allows for the library to efficiently multiplex connections under the hood and eliminates the risk that the passed-in `IDbConnection`/`IDbTransaction` gets used in a way that disrupts the locking process. **NOTE that since `IDbConnection`/`IDbTransaction` objects are not thread-safe, lock objects constructed with them can only be used by one thread at a time.**

## Options

When connecting using a `connectionString`, several other tuning options can also be specified.

- `KeepaliveCadence` configures the frequency at which an innocuous query will be issued on the connection while the lock is being held. The purpose of automatic keepalive is to prevent SQL Azure's aggressive connection governor from killing "idle" lock-holding connections. Defaults to 10 minutes.
- `UseTransaction` scopes the lock to an internally-managed transaction under the hood (otherwise it is connection-scoped). Defaults to FALSE because this mode can lead to long-running transactions which can be disruptive on databases using the full recovery model.
- `UseMultiplexing` allows the implementation to re-use connections under the hood to hold multiple locks under certain scenarios, leading to lower resource consumption. This behavior defaults to ON except in the case where `UseTransaction` is set to TRUE since the two are not compatible. You should only manually disable `UseMultiplexing` for troubleshooting purposes if you suspect it is causing issues.
