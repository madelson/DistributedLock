# DistributedLock

DistributedLock is a .NET library that provides robust and easy-to-use distributed mutexes, reader-writer locks, and semaphores based on a variety of underlying technologies.

With DistributedLock, synchronizing access to a region of code across multiple applications/machines is as simple as:
```C#
using (await myDistributedLock.AcquireAsync())
{
	// I hold the lock here
}
```

## Implementations

DistributedLock contains implementations based on various technologies; you can install implementation packages individually or just install the [DistributedLock NuGet package](https://www.nuget.org/packages/DistributedLock) [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.svg?style=flat)](https://www.nuget.org/packages/DistributedLock/), an "umbrella" package which includes all implementations as dependencies.

- **[DistributedLock.SqlServer](docs/DistributedLock.SqlServer.md)** [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.SqlServer.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.SqlServer/): uses Microsoft SQL Server
- **[DistributedLock.Postgres](docs/DistributedLock.Postgres.md)** [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.Postgres.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.Postgres/): uses Postgresql
- **[DistributedLock.Redis](docs/DistributedLock.Redis.md)** [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.Redis.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.Redis/): uses Redis
- **[DistributedLock.Azure](docs/DistributedLock.Azure.md)** [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.Azure.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.Azure/): uses Azure blobs
- **[DistributedLock.FileSystem](docs/DistributedLock.FileSystem.md)** [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.FileSystem.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.FileSystem/): uses lock files
- **[DistributedLock.WaitHandles](docs/DistributedLock.WaitHandles.md)** [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.WaitHandles.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.WaitHandles/): uses operating system global `WaitHandle`s (Windows only)

## Synchronization primitives

- Locks: provide exclusive access to a region of code
- [Reader-writer locks](docs/Reader-writer+locks.md): a lock with multiple levels of access. The lock can be held concurrently either by any number of readers or by a single writer.
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

For applications that use [dependency injection](https://en.wikipedia.org/wiki/Dependency_injection), DistributedLock's provider make it easy to separate out the specification of a lock's (or other primitive's) name from its other settings (such as a database connection string). For example in an ASP.NET Core app you might do:

```C#
// in your Startup.cs:
services.AddSingleton(_ => new PostgresDistributedSynchronizationProvider(myConnectionString));
services.AddTransient<SomeService>();

// in SomeService.cs
public class SomeService
{
	private readonly PostgresDistributedSynchronizationProvider _synchronizationProvider;

	public SomeService(PostgresDistributedSynchronizationProvider synchronizationProvider)
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

- [Interfaces](docs/Other+topics.md#interfaces)
- [Detecting handle loss](docs/Other+topics.md#detecting-handle-loss)
- [Handle abandonment](docs/Other+topics.md#handle-abandonment)
- [Safety of distributed locking](docs/Other+topics.md#safety-of-distributed-locking)

## Contributing

Contributions are welcome! If you are interested in contributing towards a new or existing issue, please let me know via comments on the issue so that I can help you get started and avoid wasted effort on your part.

## Release notes
- 2.0.0 (see [Migrating from 1.x to 2.x](docs/Other+topics.md#migrating-from-1x-to-2x))
	- Revamped package structure so that DistributedLock is now an umbrella package and each implementation technology has its own package (BREAKING CHANGE)
	- Added Postgresql-based locking (#56, DistributedLock.Postgres 1.0.0)
	- Added Redis-based locking (#24, DistributedLock.Redis 1.0.0)
	- Added Azure blob-based locking (#42, DistributedLock.Azure 1.0.0)
	- Added file-based locking (#28, DistributedLock.FileSystem 1.0.0)
	- Added provider classes for improved IOC integration (#13)
	- Added strong naming to assemblies. Thanks @pedropaulovc for contributing! (#47, BREAKING CHANGE)
	- Made lock handles implement `IAsyncDisposable` in addition to `IDisposable` #20, BREAKING CHANGE)
	- Exposed implementation-agnostic interfaces (e. g. `IDistributedLock`) for all synchronization primitives (#10)
	- Added `HandleLostToken` API for tracking if a lock's underlying connection dies (#6, BREAKING CHANGE)
	- Added SourceLink support (#57)
	- Removed `GetSafeName` API in favor of safe naming by default (BREAKING CHANGE)
	- Renamed "SystemDistributedLock" to "EventWaitHandleDistributedLock" (DistributedLock.WaitHandles 1.0.0)
	- Stopped supporting net45 (BREAKING CHANGE)
	- Removed `DbConnection` and `DbTransaction` constructors form `SqlDistributedLock`, leaving the constructors that take `IDbConnection`/`IDbTransaction` (#35, BREAKING CHANGE)
	- Changed methods returning `Task<IDisposable>` to instead return `ValueTask`, making it so that `using (@lock.AcquireAsync()) { ... } without an `await` no longer compiles (#34, BREAKING CHANGE)
	- Changed `UpgradeableLockHandle.UpgradeToWriteLock` to return `void` (#33, BREAKING CHANGE)
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
