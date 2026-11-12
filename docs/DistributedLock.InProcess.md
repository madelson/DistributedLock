# DistributedLock.InProcess

[Download the NuGet package](https://www.nuget.org/packages/DistributedLock.InProcess) [![NuGet Status](http://img.shields.io/nuget/v/DistributedLock.InProcess.svg?style=flat)](https://www.nuget.org/packages/DistributedLock.InProcess/)

The DistributedLock.InProcess package offers **non-distributed** implementations of the DistributedLock abstractions based on in-process primitives such as `SemaphoreSlim`. This is useful for single-instance deployments, local development, and tests, where you want to program against `IDistributedLockProvider` & co. without a backing service. For example:

```C#
var provider = new InProcessDistributedSynchronizationProvider();
var @lock = provider.CreateLock("MyLockName");
await using (await @lock.AcquireAsync())
{
  // I have the lock!
}
```

## APIs

- The `InProcessDistributedLock` class implements the `IDistributedLock` interface.
- The `InProcessDistributedSemaphore` class implements the `IDistributedSemaphore` interface.
- The `InProcessDistributedReaderWriterLock` class implements the `IDistributedUpgradeableReaderWriterLock` interface.
- The `InProcessDistributedSynchronizationProvider` class implements the `IDistributedLockProvider`, `IDistributedSemaphoreProvider` and `IDistributedUpgradeableReaderWriterLockProvider` interfaces.

Primitives are created through the provider; there are no public constructors.

## Implementation notes

- **These classes only coordinate within a single process.** They provide no protection across processes or machines.
- **Scopes:** primitives are cached process-wide by (scope, name). All providers constructed with the same `scope` (the default is a shared, process-wide scope) hand out coordinating primitives for the same name, even across provider instances. Providers with different scopes are fully isolated:

  ```C#
  var tenantA = new InProcessDistributedSynchronizationProvider(scope: "tenant-a");
  var tenantB = new InProcessDistributedSynchronizationProvider(scope: "tenant-b");
  // tenantA.CreateLock("x") and tenantB.CreateLock("x") do not block each other
  ```

- **Primitive types are independent:** locks, semaphores and reader-writer locks are cached separately, so a lock and a reader-writer lock (or semaphore) with the same name do not coordinate with one another. This mirrors most other providers.
- **Semaphore max count:** the max count of a named semaphore is fixed by whichever caller first creates it while it is alive.
- **Abandonment:** a handle that is never disposed is released only once it has been garbage collected and processed by a background finalizer, which runs roughly every 30 seconds. Until then the primitive remains held. For reader-writer locks, an abandoned read handle blocks writers, and a waiting writer in turn blocks new readers. Always dispose handles.
- **Reader-writer lock fairness:** the reader-writer lock is writer-preferring. Once a writer is waiting, new readers queue behind it.
- `HandleLostToken` is always `CancellationToken.None`, since an in-process handle cannot be lost.
