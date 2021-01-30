# Migrating from 1.x to 2.x

The 2.0 release of DistributedLock is a significant departure from the 1.x series in several ways. While a large percentage of source code written for 1.x should still compile against 2.x, you will almost certainly encounter some issues and this section is intended to help navigate them.

### Package structure
First off, with the addition of many new locking technologies the package has been broken up into sub-packages which can be installed independently. This allows users to avoid bloating their dependency trees by just installing the providers they actually use. If you've been using SQL Server-based locks, for example, you may want to remove the DistributedLock package and instead install just the DistributedLock.SqlServer package when you upgrade.

### Safe naming
In 1.x, it was often necessary to call an API like `SqlDistributedLock.GetSafeName()` before constructing a lock instance to ensure that the name you passed in was compatible with the underlying technology. In 2.0, safe naming is enabled by default; instead using the exact name (which is occasionally helpful if you have non-C# code trying to take the same lock) is a constructor option:

```C#
// 1.x
new SqlDistributedLock(SqlDistributedLock.GetSafeName(name), connectionString); // safe name
new SqlDistributedLock(name, connectionString); // exact name (rare)

// 2.0
new SqlDistributedLock(name, connectionString) // safe name
new SqlDistributedLock(name, connectionString, exactName: true) // exact name (rare)
```

### SystemDistributedLock renamed
1.x's `SystemDistributedLock` has been renamed to `EventWaitHandleDistributedLock` to emphasize its reliance on that Windows-only technology. Furthermore, 2.0 contains `FileDistributedLock` which offers an alternative system-scoped locking mechanism.

### SQL Server connection options
1.x offered an [enum-based approach to configuring how the lock connected to the database](https://github.com/madelson/DistributedLock/tree/release-1.5#connection-management). In 2.0, this has been replaced with a more flexible options argument. Note that Azure-focused "keepalive" behavior and connection multiplexing have been enabled by default in 2.0.

### ValueTask return values
The 1.x `AcquireAsync` methods return `Task<IDisposable>`. The problem with this API is that it made it easy to write incorrect code that looked correct because `Task` implements `IDisposable`. 2.0 switches to `ValueTask` (which wasn't available when the 1.x APIs were created) to avoid this problem. 

```C#
// 1.x
// Forgetting 'await' means that the using block is disposing the Task and not the lock handle.
// We will likely enter the block before the handle is acquired and will never release the handle!
using (myLock.AcquireAsync()) { } 

// 2.0
// this code will not compile (since ValueTask is not IDisposable)
using (myLock.AcquireAsync()) { }
```

If you need a `Task`, you can simply call `.AsTask()` on the returned `ValueTask`. If you are doing anything other than immediately awaiting the task, I recommend reading [Microsoft's documentation on how ValueTasks can be used](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1?view=net-5.0#remarks).

### Async disposal
Since the release of 1.x, Microsoft added the `IAsyncDisposable` interface as an async-friendly alternative to `IDisposable`. If you were previously using async locking, you can now switch to async disposing for additional async goodness!

```C#
// 1.x and 2.0 (acquires asynchronously, releases synchronously)
using (await myLock.AcquireAsync()) { }

// 2.0 only (acquires and releases asynchronously)
await using (await myLock.AcquireAsync()) { }
```
