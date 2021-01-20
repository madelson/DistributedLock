# DistributedLock.FileSystem

[Download the NuGet package](https://www.nuget.org/packages/DistributedLock.FileSystem) [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.FileSystem.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.FileSystem/)

The DistributedLock.FileSystem package offers distributed locks based on file handles/locks. For example:

```C#
var lockFileDirectory = new DirectoryInfo(Environment.CurrentDirectory); // choose where the lock files will live
var @lock = new FileDistributedLock(lockFileDirectory, "MyLockName");
await using (var handle = await @lock.TryAcquireAsync())
{
    if (handle != null) { /* I have the lock */ }
}
```

## APIs

- The `FileDistributedLock` class implements the `IDistributedLock` interface.
- The `FileDistributedSynchronizationProvider` class implements the `IDistributedLockProvider` interface.

## Implementation notes

Because they are based on files, these locks are used to coordinate between processes on the same machine (as opposed to across machines). In some csaes, it may be possible to coordinate across machines by specifying the path of a networked file. However, this should be tested because the network file system may not truly support locking.

`FileDistributedLock`s can be constructed either from a base `DirectoryInfo` and a `name`, which will cause it to create a file *based on* `name` in the specified directory. If you know exactly which file you'd like to lock on, you can pass a `FileInfo` instead.

Because of how exclusive file handles work in .NET, the acquire operation cannot truly block. If waiting to acquire a lock that is not available, the implementation will periodically sleep and retry until the lease can be taken or the acquire timeout elapses. Because of this, these locks are maximally efficient when using `TryAcquire` semantics with a timeout of zero.

## Options

File-based locks have no additional configuration options.


