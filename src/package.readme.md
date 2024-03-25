DistributedLock is a .NET library that provides robust and easy-to-use distributed mutexes, reader-writer locks, and semaphores based on a variety of underlying technologies.

With DistributedLock, synchronizing access to a region of code across multiple applications/machines is as simple as:
```C#
await using (await myDistributedLock.AcquireAsync())
{
	// I hold the lock here
}
```

**Read the documentation [here](https://github.com/madelson/DistributedLock).**