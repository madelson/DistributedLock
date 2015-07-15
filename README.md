# DistributedLock

DistributedLock is a lightweight .NET library that makes it easy to set up and use system-wide or fully distributed locks.

DistributedLock is available for download as a [NuGet package](https://www.nuget.org/packages/DistributedLock).

## System-wide locks

System-wide locks are great for synchronizing between processes or .NET application domains:

```C#
var myLock = new SystemDistributedLock("SystemLock");

using (myLock.Acquire())
{
	// this block of code is protected by the lock!
}
```

## Fully distributed locks

DistributedLock allows you to easily leverage MSFT SQLServer's <a href="https://msdn.microsoft.com/en-us/library/ms189823.aspx">application lock</a> functionality to provide synchronization between machines in a distributed environment. To use this functionality, you'll need a SQLServer connection string:

```C#
var connectionString = ConfigurationManager.ConnectionStrings["MyDatabase"].ConnectionString;
var myLock = new SqlDistributedLock("SqlLock", connectionString);

using (myLock.Acquire())
{
	// this block of code is protected by the lock!
}
```

As of version 1.1.0, `SqlDistributedLock`s can now be scoped to existing `DbTransaction` and/or `DbConnection` objects as an alternative to passing a connection string directly (in which case the lock manages its own connection).

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

## Release notes
- 1.1.0 Added support for SQL distributed locks scoped to existing connections/transactions
- 1.0.1 Minor fix when using infinite timeouts
- 1.0.0 Initial release
