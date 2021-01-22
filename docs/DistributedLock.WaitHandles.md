# DistributedLock.WaitHandles

[Download the NuGet package](https://www.nuget.org/packages/DistributedLock.WaitHandles) [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.Azure.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.WaitHandles/)

The DistributedLock.WaitHandles package offers distributed locks based on [global WaitHandles in Windows](https://docs.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-createeventa?redirectedfrom=MSDN). **This library only works on Windows.** For example:

```C#
var @lock = new EventWaitHandleDistributedLock("MyLockName");
await using (await @lock.AcquireAsync())
{
  // I have the lock!
}
```

## APIs

- The `EventWaitHandleDistributedLock` class implements the `IDistributedLock` interface.
- The `WaitHandleDistributedSemaphore` class implements the `IDistributedSemaphore` interface.
- The `WaitHandleDistributedSynchronizationProvider` class implements the `IDistributedLockProvider` and `IDistributedSemaphoreProvider` interfaces.

## Implementation notes

Because they are based on global `EventWaitHandle`s/`Semaphore`s, **these classes are used to coordinate between processes on the same machine** (as opposed to across machines).

## Options

The optional `abandonmentCheckCadence` argument specifies how frequently the implementation will check to see if the original holder of a lock/semaphore abandoned it without properly releasing it while waiting for it to become available. Defaults to 2s.
