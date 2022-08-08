#### [DistributedLock.MySql](README.md 'README')
### [Medallion.Threading.MySql](Medallion.Threading.MySql.md 'Medallion.Threading.MySql').[MySqlDistributedLock](MySqlDistributedLock.md 'Medallion.Threading.MySql.MySqlDistributedLock')

## MySqlDistributedLock.Acquire(Nullable<TimeSpan>, CancellationToken) Method

Acquires the lock synchronously, failing with [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException') if the attempt times out. Usage: 

```csharp
using (myLock.Acquire(...))
{
    /* we have the lock! */
}
// dispose releases the lock
```

```csharp
public Medallion.Threading.MySql.MySqlDistributedLockHandle Acquire(System.Nullable<System.TimeSpan> timeout=null, System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken));
```
#### Parameters

<a name='Medallion.Threading.MySql.MySqlDistributedLock.Acquire(System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).timeout'></a>

`timeout` [System.Nullable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')[System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')

How long to wait before giving up on the acquisition attempt. Defaults to [System.Threading.Timeout.InfiniteTimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Timeout.InfiniteTimeSpan 'System.Threading.Timeout.InfiniteTimeSpan')

<a name='Medallion.Threading.MySql.MySqlDistributedLock.Acquire(System.Nullable_System.TimeSpan_,System.Threading.CancellationToken).cancellationToken'></a>

`cancellationToken` [System.Threading.CancellationToken](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.CancellationToken 'System.Threading.CancellationToken')

Specifies a token by which the wait can be canceled

Implements [Acquire(Nullable&lt;TimeSpan&gt;, CancellationToken)](https://github.com/madelson/DistributedLock/tree/default-documentation/docs/api/DistributedLock.Core/IDistributedLock.Acquire.Q+8FXimBZqUrDv5tTRw59w.md 'Medallion.Threading.IDistributedLock.Acquire(System.Nullable{System.TimeSpan},System.Threading.CancellationToken)')

#### Returns
[MySqlDistributedLockHandle](MySqlDistributedLockHandle.md 'Medallion.Threading.MySql.MySqlDistributedLockHandle')  
A [MySqlDistributedLockHandle](MySqlDistributedLockHandle.md 'Medallion.Threading.MySql.MySqlDistributedLockHandle') which can be used to release the lock