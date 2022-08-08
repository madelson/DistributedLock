#### [DistributedLock.Redis](README.md 'README')
### [Medallion.Threading.Redis](Medallion.Threading.Redis.md 'Medallion.Threading.Redis').[RedisDistributedSynchronizationOptionsBuilder](RedisDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder')

## RedisDistributedSynchronizationOptionsBuilder.MinValidityTime(TimeSpan) Method

The lock expiry determines how long the lock will be held without being extended. However, since it takes some amount
of time to acquire the lock, we will not have all of expiry available upon acquisition.

This value sets a minimum amount which we'll be guaranteed to have left once acquisition completes.

Defaults to 90% of the specified lock expiry.

```csharp
public Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder MinValidityTime(System.TimeSpan minValidityTime);
```
#### Parameters

<a name='Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder.MinValidityTime(System.TimeSpan).minValidityTime'></a>

`minValidityTime` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

#### Returns
[RedisDistributedSynchronizationOptionsBuilder](RedisDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder')