# DistributedLock.Oracle

[Download the NuGet package](https://www.nuget.org/packages/DistributedLock.Oracle) [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.Oracle.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.Oracle/)

The DistributedLock.Oracle package offers distributed synchronization primitives based on Oracle's [DBMS_LOCK package](https://docs.oracle.com/database/121/ARPLS/d_lock.htm). For example:

```C#
var @lock = new OracleDistributedLock("MyLockName", connectionString);
using (@lock.Acquire())
{
   // I have the lock
}
```

## Setup

Because the library uses Oracle's DBMS_LOCK package under the hood, **you may need to permission your user to that package**. If you encounter an error like `identifier 'SYS.DBMS_LOCK' must be declared ORA-06550`, configure your Oracle user like so:

```SQL
connect as sys
grant execute on SYS.DBMS_LOCK to someuser;
```

See [this StackOverflow question](https://stackoverflow.com/questions/10870787/oracle-pl-sql-dbms-lock-error) for more info.

## APIs

- The `OracleDistributedLock` class implements the `IDistributedLock` interface.
- The `OracleDistributedReaderWriterLock` class implements the `IDistributedUpgradeableReaderWriterLock` interface.
- The `OracleDistributedSynchronizationProvider` class implements the `IDistributedLockProvider` and `IDistributedUpgradeableReaderWriterLockProvider` interfaces.

## Implementation notes

Oracle-based locks locks can be constructed with a connectionString or an `IDbConnection` as a means of connecting to the database. In most cases, using a connectionString is preferred because it allows for the library to efficiently multiplex connections under the hood and eliminates the risk that the passed-in `IDbConnection` gets used in a way that disrupts the locking process.

The classes in this package support async operations per the common distributed lock and ADO.NET interfaces. However, as of 2021-12-14, the Oracle .NET client libraries do not support true async IO. Therefore, if you are using the Oracle-based implementation you might get slightly better performance out of the synchronous APIs (e. g. `OracleDistributedLock.Acquire()` instead of `OracleDistributedLock.AcquireAsync()`).

## Options

In addition to specifying the `key`, several tuning options are available for `connectionString`-based locks:

- `KeepaliveCadence` allows you to have the implementation periodically issue a cheap query on a connection holding a lock. This helps in configurations which are set up to aggressively kill idle connections. Defaults to OFF (`Timeout.InfiniteTimeSpan`).
- `UseMultiplexing` allows the implementation to re-use connections under the hood to hold multiple locks under certain scenarios, leading to lower resource consumption. This behavior defaults to ON; you should not disable it unless you suspect that it is causing issues for you (please file an issue here if so!).



