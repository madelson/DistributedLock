# DistributedLock.MySql

[Download the NuGet package](https://www.nuget.org/packages/DistributedLock.MySql) [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.MySql.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.MySql/)

The DistributedLock.MySql package offers distributed synchronization primitives based on [MySQL/MariaDB user locks](https://dev.mysql.com/doc/refman/5.7/en/locking-functions.html). For example:

```C#
var @lock = new MySqlDistributedLock("mylockname"), connectionString);
await using (await @lock.AcquireAsync())
{
   // I have the lock
}
```

## APIs

- The `MySqlDistributedLock` class implements the `IDistributedLock` interface.
- The `MySqlDistributedSynchronizationProvider` class implements the `IDistributedLockProvider` and `IDistributedReaderWriterLockProvider` interfaces.

## Implementation notes

MySQL-based locks have been tested against and work with both the [MySQL](https://www.mysql.com/) and [MariaDB](https://mariadb.org/).

MySQL-based locks locks can be constructed with a `connectionString`, an `IDbConnection` or an `IDbTransaction` as a means of connecting to the database. In most cases, using a `connectionString` is preferred because it allows for the library to efficiently multiplex connections under the hood and eliminates the risk that the passed-in `IDbConnection` gets used in a way that disrupts the locking process. Using an `IDbTransaction` is generally equivalent to using an `IDbConnection` (the lock is still connection-scoped), but it allows the lock to participate in an ongoing transaction. **NOTE that since `IDbConnection`/`IDbTransaction` objects are not thread-safe, lock objects constructed with them can only be used by one thread at a time.**

Natively, MySQL's locking functions are case-insensitive with respect to the lock name. Since the DistributedLock library as a whole uses case-sensitive names, lock names containing uppercase characters will be transformed/hashed under the hood (as will empty names or names that are too long). If your program needs to coordinate with other code that is using `GET_LOCK` directly, be sure to express the name in lower case and to pass `exactName: true` when constructing the lock instance (in `exactName` mode, an invalid name will throw an exception rather than silently being transformed into a valid one).

## Options

In addition to specifying the `name`, several tuning options are available for `connectionString`-based locks:

- `KeepaliveCadence` allows you to have the implementation periodically issue a cheap query on a connection holding a lock. This helps in configurations which are set up to aggressively kill idle connections. Defaults to OFF (`Timeout.InfiniteTimeSpan`).
- `UseMultiplexing` allows the implementation to re-use connections under the hood to hold multiple locks under certain scenarios, leading to lower resource consumption. This behavior defaults to ON. **Note that this behavior must be disabled if you are using a version of MySQL older than 5.7** (see [here](https://github.com/madelson/DistributedLock/issues/123) and [here](https://dev.mysql.com/doc/refman/5.6/en/locking-functions.html) for more). Otherwise, you should not disable it unless you suspect that it is causing issues for you (please file an issue here if so!).



