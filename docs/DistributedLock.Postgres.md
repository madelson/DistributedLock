# DistributedLock.Postgres

[Download the NuGet package](https://www.nuget.org/packages/DistributedLock.Postgres) [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.Postgres.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.Postgres/)

The DistributedLock.Postgres package offers distributed synchronization primitives based on [PostgreSQL advisory locks](https://www.postgresql.org/docs/9.4/explicit-locking.html#ADVISORY-LOCKS). For example:

```C#
var @lock = new PostgresDistributedLock(new PostgresAdvisoryLockKey("MyLockName", allowHashing: true), connectionString);
await using (await @lock.AcquireAsync())
{
   // I have the lock
}
```

## APIs

- The `PostgresDistributedLock` class implements the `IDistributedLock` interface.
- The `PostgresDistributedReaderWriterLock` class implements the `IDistributedReaderWriterLock` interface.
- The `PostgresDistributedSynchronizationProvider` class implements the `IDistributedLockProvider` and `IDistributedReaderWriterLockProvider` interfaces.

## Implementation notes

Under the hood, [Postgres advisory locks can be based on either one 64-bit integer value or a pair of 32-bit integer values](https://www.postgresql.org/docs/12/functions-admin.html#FUNCTIONS-ADVISORY-LOCKS). Because of this, rather than taking in a name the lock constructors take a `PostgresAdvisoryLockKey` object which can be constructed in several ways:
- Passing a single `long` value.
- Passing a pair of `int` values.
- Passing a 16-character hex string (e. g. `"00000003ffffffff"`) which will be parsed as a `long`.
- Passing a pair of comma-separated 8-character hex strings (e. g. `"00000003,ffffffff"`) which will be parsed as a pair of `int`s.
- Passing an ASCII string with 0-9 characters, which will be mapped to a `long` based on a custom scheme.
- Passing an arbitrary string with the `allowHashing` option set to `true` which will be hashed to a `long`. Note that hashing will only be used if other methods of interpreting the string fail.

In addition to specifying the `key`, Postgres-based locks allow you to specify either a `connectionString` or an `IDbConnection` as a means of connecting to the database. In most cases, using a `connectionString` is preferred because it allows for the library to efficiently multiplex connections under the hood and eliminates the risk that the passed-in `IDbConnection` gets used in a way that disrupts the locking process. **NOTE that since `IDbConnection` objects are not thread-safe, lock objects constructed with them can only be used by one thread at a time.**

## Options

In addition to specifying the `key`, several tuning options are available for `connectionString`-based locks:

- `KeepaliveCadence` allows you to have the implementation periodically issue a cheap query on a connection holding a lock. This helps in configurations which are set up to aggressively kill idle connections. Defaults to OFF (`Timeout.InfiniteTimeSpan`).
- `UseTransaction` scopes the lock to an internally-managed transaction under the hood (otherwise it is connection-scoped). Defaults to FALSE because this mode is not compatible with multiplexing and thus consumes more connections.
- `UseMultiplexing` allows the implementation to re-use connections under the hood to hold multiple locks under certain scenarios, leading to lower resource consumption. This behavior defaults to ON; you should not disable it unless you suspect that it is causing issues for you (please file an issue here if so!).



