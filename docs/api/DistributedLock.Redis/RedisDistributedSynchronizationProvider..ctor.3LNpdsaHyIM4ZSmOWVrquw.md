#### [DistributedLock.Redis](README.md 'README')
### [Medallion.Threading.Redis](Medallion.Threading.Redis.md 'Medallion.Threading.Redis').[RedisDistributedSynchronizationProvider](RedisDistributedSynchronizationProvider.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationProvider')

## RedisDistributedSynchronizationProvider(IDatabase, Action<RedisDistributedSynchronizationOptionsBuilder>) Constructor

Constructs a [RedisDistributedSynchronizationProvider](RedisDistributedSynchronizationProvider.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationProvider') that connects to the provided [database](RedisDistributedSynchronizationProvider..ctor.3LNpdsaHyIM4ZSmOWVrquw.md#Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.RedisDistributedSynchronizationProvider(IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).database 'Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.RedisDistributedSynchronizationProvider(IDatabase, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>).database')  
and uses the provided [options](RedisDistributedSynchronizationProvider..ctor.3LNpdsaHyIM4ZSmOWVrquw.md#Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.RedisDistributedSynchronizationProvider(IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).options 'Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.RedisDistributedSynchronizationProvider(IDatabase, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>).options').

```csharp
public RedisDistributedSynchronizationProvider(IDatabase database, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>? options=null);
```
#### Parameters

<a name='Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.RedisDistributedSynchronizationProvider(IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).database'></a>

`database` [StackExchange.Redis.IDatabase](https://docs.microsoft.com/en-us/dotnet/api/StackExchange.Redis.IDatabase 'StackExchange.Redis.IDatabase')

<a name='Medallion.Threading.Redis.RedisDistributedSynchronizationProvider.RedisDistributedSynchronizationProvider(IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[RedisDistributedSynchronizationOptionsBuilder](RedisDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')