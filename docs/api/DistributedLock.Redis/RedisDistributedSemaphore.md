#### [DistributedLock.Redis](README.md 'README')
### [Medallion.Threading.Redis](Medallion.Threading.Redis.md 'Medallion.Threading.Redis')

## RedisDistributedSemaphore Class

Implements a [IDistributedSemaphore](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSemaphore.md 'Medallion.Threading.IDistributedSemaphore') using Redis.

```csharp
public sealed class RedisDistributedSemaphore :
Medallion.Threading.IDistributedSemaphore
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; RedisDistributedSemaphore

Implements [IDistributedSemaphore](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSemaphore.md 'Medallion.Threading.IDistributedSemaphore')

| Constructors | |
| :--- | :--- |
| [RedisDistributedSemaphore(RedisKey, int, IDatabase, Action&lt;RedisDistributedSynchronizationOptionsBuilder&gt;)](RedisDistributedSemaphore..ctor.uV7HEVNcvSXDYGL/CEuDZg.md 'Medallion.Threading.Redis.RedisDistributedSemaphore.RedisDistributedSemaphore(RedisKey, int, IDatabase, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>)') | Constructs a semaphore named [key](RedisDistributedSemaphore..ctor.uV7HEVNcvSXDYGL/CEuDZg.md#Medallion.Threading.Redis.RedisDistributedSemaphore.RedisDistributedSemaphore(RedisKey,int,IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).key 'Medallion.Threading.Redis.RedisDistributedSemaphore.RedisDistributedSemaphore(RedisKey, int, IDatabase, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>).key') using the provided [maxCount](RedisDistributedSemaphore..ctor.uV7HEVNcvSXDYGL/CEuDZg.md#Medallion.Threading.Redis.RedisDistributedSemaphore.RedisDistributedSemaphore(RedisKey,int,IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).maxCount 'Medallion.Threading.Redis.RedisDistributedSemaphore.RedisDistributedSemaphore(RedisKey, int, IDatabase, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>).maxCount'), [database](RedisDistributedSemaphore..ctor.uV7HEVNcvSXDYGL/CEuDZg.md#Medallion.Threading.Redis.RedisDistributedSemaphore.RedisDistributedSemaphore(RedisKey,int,IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).database 'Medallion.Threading.Redis.RedisDistributedSemaphore.RedisDistributedSemaphore(RedisKey, int, IDatabase, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>).database'), and [options](RedisDistributedSemaphore..ctor.uV7HEVNcvSXDYGL/CEuDZg.md#Medallion.Threading.Redis.RedisDistributedSemaphore.RedisDistributedSemaphore(RedisKey,int,IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).options 'Medallion.Threading.Redis.RedisDistributedSemaphore.RedisDistributedSemaphore(RedisKey, int, IDatabase, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>).options'). |

| Properties | |
| :--- | :--- |
| [MaxCount](RedisDistributedSemaphore.MaxCount.md 'Medallion.Threading.Redis.RedisDistributedSemaphore.MaxCount') | Implements [MaxCount](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSemaphore.MaxCount.md 'Medallion.Threading.IDistributedSemaphore.MaxCount') |
| [Name](RedisDistributedSemaphore.Name.md 'Medallion.Threading.Redis.RedisDistributedSemaphore.Name') | Implements [Name](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedSemaphore.Name.md 'Medallion.Threading.IDistributedSemaphore.Name') |

| Methods | |
| :--- | :--- |
| [Acquire(Nullable&lt;TimeSpan&gt;, CancellationToken)](RedisDistributedSemaphore.Acquire.05VsmfQgFRAZncIw+TCkEw.md 'Medallion.Threading.Redis.RedisDistributedSemaphore.Acquire(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Acquires a semaphore ticket synchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Usage: |
| [AcquireAsync(Nullable&lt;TimeSpan&gt;, CancellationToken)](RedisDistributedSemaphore.AcquireAsync.RuA3UY/DINCj0PXg31neVA.md 'Medallion.Threading.Redis.RedisDistributedSemaphore.AcquireAsync(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Acquires a semaphore ticket asynchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Usage: |
| [TryAcquire(TimeSpan, CancellationToken)](RedisDistributedSemaphore.TryAcquire.gnffYYV8n2g0EsmkoLRNwg.md 'Medallion.Threading.Redis.RedisDistributedSemaphore.TryAcquire(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to acquire a semaphore ticket synchronously. Usage: |
| [TryAcquireAsync(TimeSpan, CancellationToken)](RedisDistributedSemaphore.TryAcquireAsync.I7HQi6Sg7LlrmSYRDl9JHQ.md 'Medallion.Threading.Redis.RedisDistributedSemaphore.TryAcquireAsync(System.TimeSpan, System.Threading.CancellationToken)') | Attempts to acquire a semaphore ticket asynchronously. Usage: |
