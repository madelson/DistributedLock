# Semaphores

DistributedLock's implementation of distributed semaphore have an API similar to the framework's non-distributed [SemaphoreSlim](https://msdn.microsoft.com/en-us/library/system.threading.semaphoreslim(v=vs.110).aspx) class.

The semaphore acts like a lock that can be acquired by a fixed number of processes/threads simultaneously instead of a single process/thread. This capability is frequently used to "throttle" access to some resource such as a database or email server, generally with the goal of preventing it from becoming overloaded. In such cases, a distributed mutex lock is inappropriate because we do want to allow concurrent access and simply want to cap the level of concurrency. For example:

```C#
// uses the Redis implementation; others are available
var semaphore = new RedisDistributedSemaphore("ComputeDatabase", maxCount: 5, database: database);
using (semaphore.Acquire())
{
    // only 5 callers can be inside this block concurrently
    UseComputeDatabase();
}
```
