# DistributedLock

DistributedLock is a lightweight .NET library that makes it easy to set up and use system-wide or fully-distributed locks.

DistributedLock is available for download as a [NuGet package](https://www.nuget.org/packages/DistributedLock). [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.svg?style=flat)](https://www.nuget.org/packages/DistributedLock/)

[Release notes](#release-notes)

## Features

- [System-wide locks](#system-wide-locks)
- [Fully-distributed locks](#fully-distributed-locks)
- [Reader-writer locks](#reader-writer-locks)
- [Safe naming](#naming-locks)
- [Try Semantics](#trylock)
- [Async](#async)
- [Timeouts](#timeouts)
- [Cancellation](#cancellation)
- [Connection management](#connection-management)

## System-wide locks

System-wide locks are great for synchronizing between processes or .NET application domains:

```C#
var myLock = new SystemDistributedLock("SystemLock");

using (myLock.Acquire())
{
	// this block of code is protected by the lock!
}
```

## Fully-distributed locks

DistributedLock allows you to easily leverage MSFT SQLServer's <a href="https://msdn.microsoft.com/en-us/library/ms189823.aspx">application lock</a> functionality to provide synchronization between machines in a distributed environment. To use this functionality, you'll need a SQLServer connection string:

```C#
var connectionString = ConfigurationManager.ConnectionStrings["MyDatabase"].ConnectionString;
var myLock = new SqlDistributedLock("SqlLock", connectionString);

using (myLock.Acquire())
{
	// this block of code is protected by the lock!
}
```

As of version 1.1.0, `SqlDistributedLock`s can now be scoped to existing `IDbTransaction` and/or `IDbConnection` objects as an alternative to passing a connection string directly (in which case the lock manages its own connection).

## Reader-writer locks

DistributedLock contains an implementation of a distributed [reader-writer lock](https://en.wikipedia.org/wiki/Readers%E2%80%93writer_lock) with an API similar to the framework's non-distributed [ReaderWriterLockSlim](https://msdn.microsoft.com/en-us/library/system.threading.readerwriterlockslim(v=vs.110).aspx) class. Since the implementation is based on [SQLServer application locks](https://msdn.microsoft.com/en-us/library/ms189823.aspx), this can be used to synchronize across different machines.

The reader-writer lock allows for *multiple readers or one writer.* Furthermore, at most one reader can be in *upgradeable read mode*, which allows for upgrading from read mode to write mode without relinquishing the read lock. Here's an example showing how a reader-writer lock could synchronize access to a distributed cache:

```C#
class DistributedCache
{
	private readonly SqlDistributedReaderWriterLock cacheLock = 
		new SqlDistributedReaderWriterLock(connectionString);
		
	public string Get(string key)
	{
		using (this.cacheLock.AcquireReadLock())
		{
			return /* read from cache */
		}
	}
	
	public void Add(string key, string value)
	{
		using (this.cacheLock.AcquireWriteLock())
		{
			/* write to cache */
		}
	}
	
	public void AddOrUpdate(string key, string value)
	{
		using (var upgradeableHandle = this.cache.AcquireUpgradeableReadLock())
		{
			if (/* read from cache */) { return; }
			
			upgradeableHandle.UpgradeToWriteLock();
			
			/* write to cache */
		}
	}
}
``` 

## Naming locks

For all types of locks, the name of the lock defines its identity within its scope. While in general most names will work, the names are ultimately constrained by the underlying technologies used for locking. If you don't want to worry (particularly if when generating names dynamically), you can use the GetSafeLockName method each lock type to convert an arbitrary string into a consistent valid lock name:

```C#
string baseName = // arbitrary logic
var lockName = SqlDistributedLock.GetSafeLockName(baseName);
var myLock = new SqlDistributedLock(lockName);
```

## Other features

### TryLock

All locks support a "try" mechanism so that you can attempt to claim the lock without committing to it:

```C#
using (var handle = myLock.TryAcquire())
{
	if (handle != null)
	{
		// I have the lock!
	}
	else
	{
		// someone else has it!
	}
}
```

### Async

All locks support async acquisition so that you don't need to consume threads waiting for locks to become available:

```C#
using (await myLock.AcquireAsync())
{
	// this block of code is protected by the lock!
	
	// locks can be used to protect async code
	await webClient.DownloadStringAsync(...);
}
```

Note that because of this locks do not have <a href="https://msdn.microsoft.com/en-us/library/ms228964%28v=vs.110%29.aspx">thread affinity</a> unlike the Monitor and Mutex .NET synchronization classes and are not re-entrant. 

### Timeouts

All lock methods support specifying timeouts after which point TryAcquire calls will return null and Acquire calls will throw a TimeoutException:

```C#
// wait up to 5 seconds to acquire the lock
using (var handle = myLock.TryAcquire(TimeSpan.FromSeconds(5)))
{
	if (handle != null)
	{
		// I have the lock!
	}
	else
	{
		// timed out waiting for someone else to give it up
	}
}
```

### Cancellation

All lock methods support passing a <a href="https://msdn.microsoft.com/en-us/library/dd997289%28v=vs.110%29.aspx">CancellationToken</a> which, if triggered, will break out of the wait:

```
// acquire the lock, unless someone cancels us
CancellationToken token = ...
using (myLock.Acquire(cancellationToken: token))
{
	// this block of code is protected by the lock!
}
```

### Connection management

When using SQL-based locks, DistributedLock exposes several options for managing the underlying connection/transaction that scopes the lock:
- Explicit: you can pass in the IDbConnection/IDbTransaction instance that provides lock scope. This is useful when you don't have access to a connection string or
when you want the locking to be tied closely to other SQL operations being performed.
- Connection: the lock internally manages a `SqlConnection` instance. The lock is released by calling [sp_releaseapplock](https://msdn.microsoft.com/en-us/library/ms178602.aspx) after which the connection is disposed. This is the default mode.
- Transaction: the lock internally manages a `SqlTransaction` instance. The lock is released by disposing the transaction.
- Connection Multiplexing: the library internally manages a pool of `SqlConnection` instances, each of which may be used to hold multiple locks
simultaneously. This is particularly helpful for high-load scenarios since it can drastically reduce load on the underlying connection pool.
- Azure: similar to the "Connection" strategy, but also automatically issues periodic background queries on the underlying connection to keep it from looking idle to the Azure connection governor. See [#5](https://github.com/madelson/DistributedLock/issues/5) for more details.

Most of the time, you'll want to use the default connection strategy. See more details about the various strategies [here](https://github.com/madelson/DistributedLock/blob/version-1.2/DistributedLock/Sql/SqlDistributedLockConnectionStrategy.cs).

## Release notes
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
