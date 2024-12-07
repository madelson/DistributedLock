# DistributedLock

DistributedLock is a .NET library that provides robust and easy-to-use distributed mutexes, reader-writer locks, and semaphores based on a variety of underlying technologies.

With DistributedLock, synchronizing access to a region of code across multiple applications/machines is as simple as:
```C#
await using (await myDistributedLock.AcquireAsync())
{
	// I hold the lock here
}
```

## Implementations

DistributedLock contains implementations based on various technologies; you can install implementation packages individually or just install the [DistributedLock NuGet package](https://www.nuget.org/packages/DistributedLock) [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.svg?style=flat)](https://www.nuget.org/packages/DistributedLock/), a ["meta" package](https://endjin.com/blog/2020/09/streamline-dependency-management-with-nuget-meta-packages) which includes all implementations as dependencies. *Note that each package is versioned independently according to SemVer*.

- **[DistributedLock.SqlServer](docs/DistributedLock.SqlServer.md)** [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.SqlServer.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.SqlServer/) [![Static Badge](https://img.shields.io/badge/API%20Docs-DNDocs-190088?logo=readme&logoColor=white)](https://dndocs.com/d/distributedlock/api/Medallion.Threading.SqlServer.html)
: uses Microsoft SQL Server
- **[DistributedLock.Postgres](docs/DistributedLock.Postgres.md)** [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.Postgres.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.Postgres/) [![Static Badge](https://img.shields.io/badge/API%20Docs-DNDocs-190088?logo=readme&logoColor=white)](https://dndocs.com/d/distributedlock/api/Medallion.Threading.Postgres.html)
: uses Postgresql
- **[DistributedLock.MySql](docs/DistributedLock.MySql.md)** [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.MySql.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.MySql/) [![Static Badge](https://img.shields.io/badge/API%20Docs-DNDocs-190088?logo=readme&logoColor=white)](https://dndocs.com/d/distributedlock/api/Medallion.Threading.MySql.html): uses MySQL or MariaDB
- **[DistributedLock.Oracle](docs/DistributedLock.Oracle.md)** [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.Oracle.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.Oracle/) [![Static Badge](https://img.shields.io/badge/API%20Docs-DNDocs-190088?logo=readme&logoColor=white)](https://dndocs.com/d/distributedlock/api/Medallion.Threading.Oracle.html): uses Oracle
- **[DistributedLock.Redis](docs/DistributedLock.Redis.md)** [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.Redis.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.Redis/) [![Static Badge](https://img.shields.io/badge/API%20Docs-DNDocs-190088?logo=readme&logoColor=white)](https://dndocs.com/d/distributedlock/api/Medallion.Threading.Redis.html): uses Redis
- **[DistributedLock.Azure](docs/DistributedLock.Azure.md)** [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.Azure.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.Azure/) [![Static Badge](https://img.shields.io/badge/API%20Docs-DNDocs-190088?logo=readme&logoColor=white)](https://dndocs.com/d/distributedlock/api/Medallion.Threading.Azure.html): uses Azure blobs
- **[DistributedLock.ZooKeeper](docs/DistributedLock.ZooKeeper.md)** [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.ZooKeeper.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.ZooKeeper/) [![Static Badge](https://img.shields.io/badge/API%20Docs-DNDocs-190088?logo=readme&logoColor=white)](https://dndocs.com/d/distributedlock/api/Medallion.Threading.ZooKeeper.html): uses Apache ZooKeeper
- **[DistributedLock.FileSystem](docs/DistributedLock.FileSystem.md)** [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.FileSystem.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.FileSystem/) [![Static Badge](https://img.shields.io/badge/API%20Docs-DNDocs-190088?logo=readme&logoColor=white)](https://dndocs.com/d/distributedlock/api/Medallion.Threading.FileSystem.html): uses lock files
- **[DistributedLock.WaitHandles](docs/DistributedLock.WaitHandles.md)** [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.WaitHandles.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.WaitHandles/) [![Static Badge](https://img.shields.io/badge/API%20Docs-DNDocs-190088?logo=readme&logoColor=white)](https://dndocs.com/d/distributedlock/api/Medallion.Threading.WaitHandles.html): uses operating system global `WaitHandle`s (Windows only)

**Click on the name** of any of the above packages to see the documentation specific to that implementation, or read on for general documentation that applies to all implementations.

The [DistributedLock.Core](https://www.nuget.org/packages/DistributedLock.Core) [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.Core.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.Core/) package contains common code and abstractions and is referenced by all implementations.

## Synchronization primitives

- Locks: provide exclusive access to a region of code
- [Reader-writer locks](docs/Reader-writer%20locks.md): a lock with multiple levels of access. The lock can be held concurrently either by any number of "readers" or by a single "writer".
- [Semaphores](docs/Semaphores.md): similar to a lock, but can be held by up to N users concurrently instead of just one.

While all implementations support locks, the other primitives are only supported by some implementations. See the [implementation-specific documentation pages](docs) for details.

## Basic usage

### Names

Because distributed locks (and other distributed synchronization primitives) are not isolated to a single process, their identity is based on their name which is provided through the constructor. Different underlying technologies have different restrictions on name format; however, DistributedLock largely allows you to ignore these by escaping/hashing names that would otherwise be invalid.

### Acquire

All synchronization primitives support the same basic access pattern. The `Acquire` method returns a "handle" object that represents holding the lock. When the handle is disposed, the lock is released:

```C#
var myDistributedLock = new SqlDistributedLock(name, connectionString); // e. g. if we are using SQL Server
using (myDistributedLock.Acquire())
{
	// we hold the lock here
} // implicit Dispose() call from using block releases it here
```

### TryAcquire

While `Acquire` will block until the lock is available, there is also a `TryAcquire` variant which returns `null` if the lock could not be acquired (due to being held elsewhere):

```C#
using (var handle = myDistributedLock.TryAcquire())
{
	if (handle != null)
	{
		// we acquired the lock :-)
	}
	else
	{
		// someone else has it :-(
	}
}
```

### async support

`async` versions of both of these methods are also supported. These are preferred when you are writing async code since they will not consume a thread while waiting for the lock. If you are using C#8 or higher, you can also dispose of handles asynchronously:

### Timeouts

```C#
await using (await myDistributedLock.AcquireAsync()) { ... }
```

Additionally, all of these methods support an optional `timeout` parameter. `timeout` determines how long `Acquire` will wait before failing with a `TimeoutException` and how long `TryAcquire` will wait before returning null. The default `timeout` for `Acquire` is `Timeout.InfiniteTimeSpan` while for `TryAcquire` the default `timeout` is `TimeSpan.Zero`.

### Cancellation

Finally, the methods take an optional `CancellationToken` parameter, which allows for the acquire operation to be interrupted via cancellation. Note that this won't cancel the hold on the lock once the acquire succeeds.

## Providers

For applications that use [dependency injection](https://en.wikipedia.org/wiki/Dependency_injection), DistributedLock's providers make it easy to separate out the specification of a lock's (or other primitive's) name from its other settings (such as a database connection string). For example in an ASP.NET Core app you might do:

```C#
// in your Startup.cs:
services.AddSingleton<IDistributedLockProvider>(_ => new PostgresDistributedSynchronizationProvider(myConnectionString));
services.AddTransient<SomeService>();

// in SomeService.cs
public class SomeService
{
	private readonly IDistributedLockProvider _synchronizationProvider;

	public SomeService(IDistributedLockProvider synchronizationProvider)
	{
		this._synchronizationProvider = synchronizationProvider;
	}
	
	public void InitializeUserAccount(int id)
	{
		// use the provider to construct a lock
		var @lock = this._synchronizationProvider.CreateLock($"UserAccount{id}");
		using (@lock.Acquire())
		{
			// do stuff
		}
		
		// ALTERNATIVELY, for common use-cases extension methods allow this to be done with a single call
		using (this._synchronizationProvider.AcquireLock($"UserAccount{id}"))
		{
			// do stuff
		}
	}
}
```

## Other topics

- [Interfaces](docs/Other%20topics.md#interfaces)
- [Detecting handle loss](docs/Other%20topics.md#detecting-handle-loss)
- [Handle abandonment](docs/Other%20topics.md#handle-abandonment)
- [Safety of distributed locking](docs/Other%20topics.md#safety-of-distributed-locking)

## Contributing

Contributions are welcome! If you are interested in contributing towards a new or existing issue, please let me know via comments on the issue so that I can help you get started and avoid wasted effort on your part.

Setup steps for working with the repository locally are documented [here](docs/Developing%20DistributedLock.md).

## Release notes
- 2.5.1
	- Increase efficiency of Azure blob locks when the blob does not exist. Thanks [@richardkooiman](https://github.com/richardkooiman) for implementing! ([#227](https://github.com/madelson/DistributedLock/pull/227), DistributedLock.Azure 1.0.2)
	- Improve error handling in race condition scenarios for Azure blobs. Thanks [@MartinDembergerR9](https://github.com/MartinDembergerR9) for implementing! ([#228](https://github.com/madelson/DistributedLock/pull/228), DistributedLock.Azure 1.0.2)
	- Bump Microsoft.Data.SqlClient to 5.2.2 to avoid vulnerability. Thanks [@steve85](https://github.com/steve85) for implementing! ([#229](https://github.com/madelson/DistributedLock/pull/229), DistributedLock.SqlServer 1.0.6)
	- Bump Oracle.ManagedDataAccess to latest to avoid bringing in vulnerable packages (DistributedLock.Core 1.0.8, DistributedLock.Oracle 1.0.4)
	- Bump Npgsql to latest patch to avoid bringing in vulnerable packages (DistributedLock.Postgres 1.2.1)
- 2.5
	- Add support for creating Postgres locks off `DbDataSource` which is helpful for apps using `NpgsqlMultiHostDataSource`. Thanks [davidngjy](https://github.com/davidngjy) for implementing! ([#153](https://github.com/madelson/DistributedLock/issues/153), DistributedLock.Postgres 1.2.0)
	- Upgrade Npgsql to 8.0.3 to avoid vulnerability. Thanks [@Meir017](https://github.com/Meir017)/[@davidngjy](https://github.com/davidngjy) for implementing! ([#218](https://github.com/madelson/DistributedLock/issues/218), DistributedLock.Postgres 1.2.0)
	- Fix Postgres race condition with connection keepalive enabled ([#216](https://github.com/madelson/DistributedLock/issues/216), DistributedLock.Core 1.0.7)
	- Upgrade Microsoft.Data.SqlClient to 5.2.1 to avoid vulnerability ([#210](https://github.com/madelson/DistributedLock/issues/210), DistributedLock.SqlServer 1.0.5)
	- Improve directory creation concurrency handling for `FileDistributedLock` (DistributedLock.FileSystem 1.0.3) 
- 2.4
	- Add support for transaction-scoped locking in Postgres using `pg_advisory_xact_lock` which is helpful when using PgBouncer ([#168](https://github.com/madelson/DistributedLock/issues/168), DistributedLock.Postgres 1.1.0)
	- Improve support for newer versions of StackExchange.Redis, especially when using the default backlog policy ([#162](https://github.com/madelson/DistributedLock/issues/162), DistributedLock.Redis 1.0.3). Thanks [@Bartleby2718](https://github.com/Bartleby2718) for helping with this!
	- Drop `net461` support (`net462` remains supported). Thanks [@Bartleby2718](https://github.com/Bartleby2718) for implementing! 
	- Reduce occurrence of `UnobservedTaskException`s thrown by the library ([#192](https://github.com/madelson/DistributedLock/issues/192), DistributedLock.Core 1.0.6)
	- Update dependencies to modern versions without known issues/vulnerabilities ([#111](https://github.com/madelson/DistributedLock/issues/111)/[#177](https://github.com/madelson/DistributedLock/issues/177)/[#184](https://github.com/madelson/DistributedLock/issues/184)/[#185](https://github.com/madelson/DistributedLock/issues/185), all packages). Thanks [@Bartleby2718](https://github.com/Bartleby2718) for helping with this!
	- Improve directory creation concurrency handling for `FileDistributedLock` on Linux/.NET 8 ([#195](https://github.com/madelson/DistributedLock/issues/195), DistributedLock.FileSystem 1.0.2)
	- Allow using transaction-scoped locks in SQL Server without explicitly disabling multiplexing ([#189](https://github.com/madelson/DistributedLock/issues/189), DistributedLock.SqlServer 1.0.4)
	- New API documentation on [dndocs](https://dndocs.com/). Thanks [@NeuroXiq](https://github.com/NeuroXiq)!
	- New documentation for contributors to get the project running locally (see [Contributing](#contributing))
- 2.3.4
	- Support Npgsql 8.0's [ExecuteScalar breaking change](https://github.com/npgsql/npgsql/issues/5143) ([#174](https://github.com/madelson/DistributedLock/issues/174), DistributedLock.Postgres 1.0.5). Thanks [@Kaffeetasse](https://github.com/Kaffeetasse) for diagnosing and fixing!
- 2.3.3
	- Update Microsoft.Data.SqlClient due to vulnerabilities ([#149](https://github.com/madelson/DistributedLock/issues/149), DistributedLock.SqlServer 1.0.3)
	- Update versions of Oracle.ManagedDataAccess and Oracle.ManagedDataAccess.Core due to vulnerabilities (DistributedLock.Oracle 1.0.2)
- 2.3.2
	- Work around underlying Postgres race condition when waiting on advisory locks with a short non-zero timeout ([#147](https://github.com/madelson/DistributedLock/issues/147), DistributedLock.Postgres 1.0.4). Thanks [@Tzachi009](https://github.com/Tzachi009) for reporting and isolating the issue!
- 2.3.1
	- Fixed concurrency issue with `HandleLostToken` for relational database locks ([#133](https://github.com/madelson/DistributedLock/issues/133), DistributedLock.Core 1.0.5, DistributedLock.MySql 1.0.1, DistributedLock.Oracle 1.0.1, DistributedLock.Postgres 1.0.3, DistributedLock.SqlServer 1.0.2). Thanks [@OskarKlintrot](https://github.com/OskarKlintrot) for testing!
	- Fixed misleading error message why trying to disable auto-extension in Redis ([#130](https://github.com/madelson/DistributedLock/issues/130), DistributedLock.Redis 1.0.2)
	- Fixed concurrency issue with canceling async waits on `WaitHandle`s ([#120](https://github.com/madelson/DistributedLock/issues/120), DistributedLock.WaitHandles 1.0.1)
- 2.3.0
	- Added Oracle-based implementation ([#45](https://github.com/madelson/DistributedLock/issues/45), DistributedLock.Oracle 1.0.0). Thanks [@odin568](https://github.com/odin568) for testing!
	- Made file-based locking more robust to transient `UnauthorizedAccessException`s ([#106](https://github.com/madelson/DistributedLock/issues/106) & [#109](https://github.com/madelson/DistributedLock/issues/109), DistributedLock.FileSystem 1.0.1)
	- Work around cancellation bug in Npgsql command preparation ([#112](https://github.com/madelson/DistributedLock/issues/112), DistributedLock.Postgres 1.0.2)
- 2.2.0
	- Added MySQL/MariaDB-based implementation ([#95](https://github.com/madelson/DistributedLock/issues/95), DistributedLock.MySql 1.0.0). Thanks [@theplacefordev](https://github.com/theplacefordev) for testing!
- 2.1.0
	- Added ZooKeeper-based implementation ([#41](https://github.com/madelson/DistributedLock/issues/41), DistributedLock.ZooKeeper 1.0.0)
- 2.0.2
	- Fixed bug where `HandleLostToken` would hang when accessed on a SqlServer or Postgres lock handle that used keepalive ([#85](https://github.com/madelson/DistributedLock/issues/85), DistributedLock.Core 1.0.1)
	- Fixed bug where broken database connections could result in future lock attempts failing when using SqlServer or Postgres locks with multiplexing ([#83](https://github.com/madelson/DistributedLock/issues/83), DistributedLock.Core 1.0.1)
	- Updated Npgsql dependency to 5.x to take advantage of various bugfixes ([#61](https://github.com/madelson/DistributedLock/issues/61), DistributedLock.Postgres 1.0.1)
- 2.0.1
	- Fixed Redis lock behavior when using a database with `WithKeyPrefix` ([#66](https://github.com/madelson/DistributedLock/issues/66), DistributedLock.Redis 1.0.1). Thanks [@skomis-mm](https://github.com/skomis-mm) for contributing!
- 2.0.0 (see also [Migrating from 1.x to 2.x](docs/Migrating%20from%201.x%20to%202.x.md#migrating-from-1x-to-2x))
	- Revamped package structure so that DistributedLock is now an umbrella package and each implementation technology has its own package (BREAKING CHANGE)
	- Added Postgresql-based locking ([#56](https://github.com/madelson/DistributedLock/issues/56), DistributedLock.Postgres 1.0.0)
	- Added Redis-based locking ([#24](https://github.com/madelson/DistributedLock/issues/24), DistributedLock.Redis 1.0.0)
	- Added Azure blob-based locking ([#42](https://github.com/madelson/DistributedLock/issues/42), DistributedLock.Azure 1.0.0)
	- Added file-based locking ([#28](https://github.com/madelson/DistributedLock/issues/28), DistributedLock.FileSystem 1.0.0)
	- Added provider classes for improved IOC integration ([#13](https://github.com/madelson/DistributedLock/issues/13))
	- Added strong naming to assemblies. Thanks [@pedropaulovc](https://github.com/pedropaulovc) for contributing! ([#47](https://github.com/madelson/DistributedLock/issues/47), BREAKING CHANGE)
	- Made lock handles implement `IAsyncDisposable` in addition to `IDisposable` [#20](https://github.com/madelson/DistributedLock/issues/20), BREAKING CHANGE)
	- Exposed implementation-agnostic interfaces (e. g. `IDistributedLock`) for all synchronization primitives ([#10](https://github.com/madelson/DistributedLock/issues/10))
	- Added `HandleLostToken` API for tracking if a lock's underlying connection dies ([#6](https://github.com/madelson/DistributedLock/issues/6), BREAKING CHANGE)
	- Added SourceLink support ([#57](https://github.com/madelson/DistributedLock/issues/57))
	- Removed `GetSafeName` API in favor of safe naming by default (BREAKING CHANGE)
	- Renamed "SystemDistributedLock" to "EventWaitHandleDistributedLock" (DistributedLock.WaitHandles 1.0.0)
	- Stopped supporting net45 (BREAKING CHANGE)
	- Removed `DbConnection` and `DbTransaction` constructors form `SqlDistributedLock`, leaving the constructors that take `IDbConnection`/`IDbTransaction` ([#35](https://github.com/madelson/DistributedLock/issues/35), BREAKING CHANGE)
	- Changed methods returning `Task<IDisposable>` to instead return `ValueTask`, making it so that `using (@lock.AcquireAsync()) { ... } without an `await` no longer compiles (#34, BREAKING CHANGE)
	- Changed `UpgradeableLockHandle.UpgradeToWriteLock` to return `void` ([#33](https://github.com/madelson/DistributedLock/issues/33), BREAKING CHANGE)
	- Switched to Microsoft.Data.SqlClient by default for all target frameworks (BREAKING CHANGE)
	- Changed all locking implementations to be non-reentrant (BREAKING CHANGE)	
- 1.5.0
	- Added cross-platform support via Microsoft.Data.SqlClient ([#25](https://github.com/madelson/DistributedLock/issues/25)). This feature is available for .NET Standard >= 2.0. Thanks to [@alesebi91](https://github.com/alesebi91) for helping with the implementation and testing!
	- Added C#8 nullable annotations ([#31](https://github.com/madelson/DistributedLock/issues/31))
	- Fixed minor bug in connection multiplexing which could lead to more lock contention ([#32](https://github.com/madelson/DistributedLock/issues/32))
- 1.4.0
	- Added a SQL-based distributed semaphore ([#7](https://github.com/madelson/DistributedLock/issues/7))
	- Fix bug where SqlDistributedLockConnectionStrategy.Azure would leak connections, relying on GC to reclaim them ([#14](https://github.com/madelson/DistributedLock/issues/14)). Thanks [zavalita1](https://github.com/zavalita1) for investigating this issue!
	- Throw a specific exception type (`DeadlockException`) rather than the generic `InvalidOperationException` when a deadlock is detected ([#11](https://github.com/madelson/DistributedLock/issues/11))
- 1.3.1 Minor fix to avoid "leaking" isolation level changes in transaction-based locks ([#8](https://github.com/madelson/DistributedLock/issues/8)). Also switched to the VS2017 project file format
- 1.3.0 Added an Azure connection strategy to keep lock connections from becoming idle and being reclaimed by Azure's connection governor ([#5](https://github.com/madelson/DistributedLock/issues/5))
- 1.2.0
	- Added a SQL-based distributed reader-writer lock
	- .NET Core support via .NET Standard
	- Changed the default locking scope for SQL distributed lock to be a connection rather than a transaction, avoiding cases where long-running transactions can block backups
	- Allowed for customization of the SQL distributed lock connection strategy when connecting via a connection string
	- Added a new connection strategy which allows for multiplexing multiple held locks onto one connection
	- Added IDbConnection/IDbTransaction constructors ([#3](https://github.com/madelson/DistributedLock/issues/3))
- 1.1.0 Added support for SQL distributed locks scoped to existing connections/transactions
- 1.0.1 Minor fix when using infinite timeouts
- 1.0.0 Initial release
