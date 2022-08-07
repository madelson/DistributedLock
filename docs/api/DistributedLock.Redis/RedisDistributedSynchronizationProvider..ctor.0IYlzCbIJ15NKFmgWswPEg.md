#### [DistributedLock.Redis](README.md 'README')
### [Medallion.Threading.Redis](Medallion.Threading.Redis.md 'Medallion.Threading.Redis').[RedisDistributedSynchronizationProvider](RedisDistributedSynchronizationProvider.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationProvider')

## RedisDistributedSynchronizationProvider(IEnumerable<IDatabase>, Action<RedisDistributedSynchronizationOptionsBuilder>) Constructor

Constructs a [RedisDistributedSynchronizationProvider](RedisDistributedSynchronizationProvider.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationProvider') that connects to the provided [databases](RedisDistributedSynchronizationProvider..ctor.0IYlzCbIJ15NKFmgWswPEg.md#Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.RedisDistributedSynchronizationProvider(System.Collections.Generic.IEnumerable_IDatabase_,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).databases 'Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.RedisDistributedSynchronizationProvider(System.Collections.Generic.IEnumerable<IDatabase>, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>).databases')  
and uses the provided [options](RedisDistributedSynchronizationProvider..ctor.0IYlzCbIJ15NKFmgWswPEg.md#Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.RedisDistributedSynchronizationProvider(System.Collections.Generic.IEnumerable_IDatabase_,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).options 'Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.RedisDistributedSynchronizationProvider(System.Collections.Generic.IEnumerable<IDatabase>, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>).options').  
  
Note that if multiple [StackExchange.Redis.IDatabase](https://docs.microsoft.com/en-us/dotnet/api/StackExchange.Redis.IDatabase 'StackExchange.Redis.IDatabase')s are provided, [CreateSemaphore(RedisKey, int)](RedisDistributedSynchronizationProvider.CreateSemaphore.ChIw2HaeWq+X0D2xMfTfjg.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.CreateSemaphore(RedisKey, int)') will use only the first  
[StackExchange.Redis.IDatabase](https://docs.microsoft.com/en-us/dotnet/api/StackExchange.Redis.IDatabase 'StackExchange.Redis.IDatabase').

```csharp
public RedisDistributedSynchronizationProvider(System.Collections.Generic.IEnumerable<IDatabase> databases, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>? options=null);
```
#### Parameters

<a name='Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.RedisDistributedSynchronizationProvider(System.Collections.Generic.IEnumerable_IDatabase_,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).databases'></a>

`databases` [System.Collections.Generic.IEnumerable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.IEnumerable-1 'System.Collections.Generic.IEnumerable`1')[StackExchange.Redis.IDatabase](https://docs.microsoft.com/en-us/dotnet/api/StackExchange.Redis.IDatabase 'StackExchange.Redis.IDatabase')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.IEnumerable-1 'System.Collections.Generic.IEnumerable`1')

<a name='Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.RedisDistributedSynchronizationProvider(System.Collections.Generic.IEnumerable_IDatabase_,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[RedisDistributedSynchronizationOptionsBuilder](RedisDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')