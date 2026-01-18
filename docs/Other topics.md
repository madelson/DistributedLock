## Interfaces

The underlying technology behind a particular locking primitive (e. g. SQL Server vs. the file system) affects its behavior and performance. Therefore, in most cases you'll want to use the specific concrete classes (e. g. `SqlDistributedLock`) when writing code with distributed locks.

However, in some cases you may want to write code that is agnostic to the specific locking technology. For example, swapping out a fully-distributed implementation for a local-system-based or mock implementation during testing might improve test performance and simplify setup.

For purposes such as this, DistributedLock provides an interface for each of the primitives that can be used in place of the concrete classes. These are: `IDistributedLock`, `IDistributedReaderWriterLock`, `IDistributedUpgradeableReaderWriterLock`, and `IDistributedSemaphore`.

Similarly, there is a set of interfaces defined for locking providers: `IDistributedLockProvider`, `IDistributedReaderWriterLockProvider`, `IDistributedUpgradeableReaderWriterLockProvider`, and `IDistributedSemaphoreProvider`. Each technology typically has a single provider class which implements all appropriate interfaces.

## Detecting handle loss

Sometimes, your code's hold on a lock can be disrupted due to a disruption in the underlying technology. For example, if you are holding a Postgres-based lock and the underlying database connection is killed, your code will no longer be holding the lock. Most such disruptions will result in a failure when the lock handle is disposed, but some may not.

In most cases, this sort of disruption is rare and not worth worrying about. However, some lock types allow for early detection of such problems through the `HandleLostToken` interface. This is a `CancellationToken` on the returned lock handle which will be canceled if the handle detects that its hold on the lock has been disrupted. **Accessing the HandleLostToken can force a handle to perform additional background work under the hood** (e. g. polling), so don't use this feature unless you think you need it.

```C#
using var handle = myLock.Acquire();

if (!handle.HandleLostToken.CanBeCanceled) { Console.WriteLine("Implementation does not support lost handle detection"); }

handle.HandeLostToken.Register(() => Console.WriteLine("Lock was lost!"));
```

## Handle abandonment

Any code that acquires a distributed lock or other primitive should be sure to dispose of it upon completion of its work to ensure that other parts of the system are not blocked.

However, in a large and complex system there is always risk that this doesn't happen, either through sloppily written code, a bug that causes an exception to occur in an unexpected place, or the handle-holding process crashing.

To provide additional protection against the "leaking" of lock handles, DistributedLock's primitives are designed so that a handle being garbage collected without being disposed or a handle-holding process exiting unexpectedly **will not cause a lock to be held forever**. This helps ensure that systems built on distributed locking are robust to unexpected failures.

## Composite locking

Sometimes, you need to acquire multiple fine-grained locks in an all-or-nothing manner (e.g. acquiring 2 per-account locks before doing an operation that affects both). Since DistributedLock.Core 1.0.9, the library now supports this via provider extension methods. For example:

```
IDistributedLockProvider provider = ...
await using (var handle = await provider.TryAcquireAllLocksAsync(new[] { "lockName1", "lockName2", ... }, timeout, cancellationToken))
{
    if (handle != null)
    {
        // all locks successfully acquired!
    }
}
```

An equivalent operation is supported for read locks, write locks, and semaphores.

NOTE: the locks will be acquired in the order provided. It is up to the caller to ensure ordering consistency across operations to prevent deadlocks (e.g. you might sort the lock names before acquiring).

## Safety of distributed locking

Distributed locking is one of the easiest ways to add robustness to a distributed system without overly-complex design. However, the nature of the approach means that for certain scenarios it may not always be the best fit.

For example, whenever we are using one technology to protect access to another technology, there is a (likely very small) risk that the locking technology suffers an outage (see section on detecting handle loss), and briefly allows concurrent access to a resource if it comes back online and starts granting new handles while old lost handles are still in use. Timeout-based locking approaches such as Redis locks and Azure leases have an inherent risk that an extended hang on the machine holding the lock could cause the timeout to expire before the lock can be automatically-renewed (a network outage could cause the same issue).

In some cases, tying together the locking technology with the underlying resource can provide additional safety. For example, when using a SQLServer or Postgres lock to protect a resource on the same database it is possible to use the same `DbConnection` for both the locking operation and the data modification. Combined with database transactions, this guarantees the integrity of the locking.

In other cases, this sort of unification isn't possible. If any violation of the locking guarantees is unacceptable, you may have to consider more complex approaches such as the techniques discussed in [this article](https://martin.kleppmann.com/2016/02/08/how-to-do-distributed-locking.html). In many cases, you simply won't be able to achieve true safety because of constraints driven by the resources you are trying to protect.

As mentioned at the start, the distributed locking approaches offered by this library are, in my experience, good enough for a large number of real-life scenarios. Furthermore, they are easy to use correctly and easy to reason about. However, it is worth being aware of any technology's limitations!