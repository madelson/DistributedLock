#### [DistributedLock.Redis](README.md 'README')
### [Medallion.Threading.Redis](Medallion.Threading.Redis.md 'Medallion.Threading.Redis').[RedisDistributedSynchronizationProvider](RedisDistributedSynchronizationProvider.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationProvider')

## RedisDistributedSynchronizationProvider.CreateSemaphore(RedisKey, int) Method

Creates a [RedisDistributedSemaphore](RedisDistributedSemaphore.md 'Medallion.Threading.Redis.RedisDistributedSemaphore') using the provided [key](RedisDistributedSynchronizationProvider.CreateSemaphore.ChIw2HaeWq+X0D2xMfTfjg.md#Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.CreateSemaphore(RedisKey,int).key 'Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.CreateSemaphore(RedisKey, int).key') and [maxCount](RedisDistributedSynchronizationProvider.CreateSemaphore.ChIw2HaeWq+X0D2xMfTfjg.md#Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.CreateSemaphore(RedisKey,int).maxCount 'Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.CreateSemaphore(RedisKey, int).maxCount').

```csharp
public Medallion.Threading.Redis.RedisDistributedSemaphore CreateSemaphore(RedisKey key, int maxCount);
```
#### Parameters

<a name='Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.CreateSemaphore(RedisKey,int).key'></a>

`key` [StackExchange.Redis.RedisKey](https://docs.microsoft.com/en-us/dotnet/api/StackExchange.Redis.RedisKey 'StackExchange.Redis.RedisKey')

<a name='Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.CreateSemaphore(RedisKey,int).maxCount'></a>

`maxCount` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')

#### Returns
[RedisDistributedSemaphore](RedisDistributedSemaphore.md 'Medallion.Threading.Redis.RedisDistributedSemaphore')