#### [DistributedLock.Postgres](README.md 'README')
### [Medallion.Threading.Postgres](Medallion.Threading.Postgres.md 'Medallion.Threading.Postgres').[PostgresDistributedReaderWriterLock](PostgresDistributedReaderWriterLock.md 'Medallion.Threading.Postgres.PostgresDistributedReaderWriterLock')

## PostgresDistributedReaderWriterLock.AcquireWriteLockAsync(Nullable<TimeSpan>, CancellationToken) Method

Acquires a WRITE lock asynchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Not compatible with another WRITE lock or an UPGRADE lock. Usage: 

```csharp
await using (await myLock.AcquireWriteLockAsync(...))
{
    /* we have the lock! */
}
// dispose releases the lock
```

```csharp
public System.Threading.Tasks.ValueTask<Medallion.Threading.Postgres.PostgresDistributedReaderWriterLockHandle> AcquireWriteLockAsync(System.Nullable<System.TimeSpan> timeout=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.Postgres.PostgresDistributedReaderWriterLock.AcquireWriteLockAsync(System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.Nullable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')[System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')

How long to wait before giving up on the acquisition attempt. Defaults to [System.Threading.Timeout.InfiniteTimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Timeout.InfiniteTimeSpan 'System.Threading.Timeout.InfiniteTimeSpan')

<a name='Medallion.Threading.Postgres.PostgresDistributedReaderWriterLock.AcquireWriteLockAsync(System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [AcquireWriteLockAsync(Nullable&lt;TimeSpan&gt;, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedReaderWriterLock.AcquireWriteLockAsync.fFyCc0HswQXUnGvjzNHJ+A.md 'Medallion.Threading.IDistributedReaderWriterLock.AcquireWriteLockAsync(System.Nullable{System.TimeSpan},System.Threading.CancellationToken)')

#### Returns
[System.Threading.Tasks.ValueTask&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')[PostgresDistributedReaderWriterLockHandle](PostgresDistributedReaderWriterLockHandle.md 'Medallion.Threading.Postgres.PostgresDistributedReaderWriterLockHandle')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Tasks.ValueTask-1 'System.Threading.Tasks.ValueTask`1')  
A [PostgresDistributedReaderWriterLockHandle](PostgresDistributedReaderWriterLockHandle.md 'Medallion.Threading.Postgres.PostgresDistributedReaderWriterLockHandle') which can be used to release the lock