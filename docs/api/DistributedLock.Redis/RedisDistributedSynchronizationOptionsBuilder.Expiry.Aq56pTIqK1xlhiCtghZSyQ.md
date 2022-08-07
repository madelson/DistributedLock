#### [DistributedLock.Redis](README.md 'README')
### [Medallion.Threading.Redis](Medallion.Threading.Redis.md 'Medallion.Threading.Redis').[RedisDistributedSynchronizationOptionsBuilder](RedisDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder')

## RedisDistributedSynchronizationOptionsBuilder.Expiry(TimeSpan) Method

Specifies how long the lock will last, absent auto-extension. Because auto-extension exists,  
this value generally will have little effect on program behavior. However, making the expiry longer means that  
auto-extension requests can occur less frequently, saving resources. On the other hand, when a lock is abandoned  
without explicit release (e. g. if the holding process crashes), the expiry determines how long other processes  
would need to wait in order to acquire it.  
  
Defaults to 30s.

```csharp
public Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder Expiry(System.TimeSpan expiry);
```
#### Parameters

<a name='Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder.Expiry(System.TimeSpan).expiry'></a>

`expiry` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

#### Returns
[RedisDistributedSynchronizationOptionsBuilder](RedisDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder')