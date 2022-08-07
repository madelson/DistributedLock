#### [DistributedLock.Redis](README.md 'README')
### [Medallion.Threading.Redis](Medallion.Threading.Redis.md 'Medallion.Threading.Redis').[RedisDistributedReaderWriterLock](RedisDistributedReaderWriterLock.md 'Medallion.Threading.Redis.RedisDistributedReaderWriterLock')

## RedisDistributedReaderWriterLock(string, IDatabase, Action<RedisDistributedSynchronizationOptionsBuilder>) Constructor

Constructs a lock named [name](RedisDistributedReaderWriterLock..ctor.UjOB67V/r8BOTbBDgotYOw.md#Medallion.Threading.Redis.RedisDistributedReaderWriterLock.RedisDistributedReaderWriterLock(string,IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).name 'Medallion.Threading.Redis.RedisDistributedReaderWriterLock.RedisDistributedReaderWriterLock(string, IDatabase, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>).name') using the provided [database](RedisDistributedReaderWriterLock..ctor.UjOB67V/r8BOTbBDgotYOw.md#Medallion.Threading.Redis.RedisDistributedReaderWriterLock.RedisDistributedReaderWriterLock(string,IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).database 'Medallion.Threading.Redis.RedisDistributedReaderWriterLock.RedisDistributedReaderWriterLock(string, IDatabase, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>).database') and [options](RedisDistributedReaderWriterLock..ctor.UjOB67V/r8BOTbBDgotYOw.md#Medallion.Threading.Redis.RedisDistributedReaderWriterLock.RedisDistributedReaderWriterLock(string,IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).options 'Medallion.Threading.Redis.RedisDistributedReaderWriterLock.RedisDistributedReaderWriterLock(string, IDatabase, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>).options').

```csharp
public RedisDistributedReaderWriterLock(string name, IDatabase database, System.Action<Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder>? options=null);
```
#### Parameters

<a name='Medallion.Threading.Redis.RedisDistributedReaderWriterLock.RedisDistributedReaderWriterLock(string,IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.Redis.RedisDistributedReaderWriterLock.RedisDistributedReaderWriterLock(string,IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).database'></a>

`database` [StackExchange.Redis.IDatabase](https://docs.microsoft.com/en-us/dotnet/api/StackExchange.Redis.IDatabase 'StackExchange.Redis.IDatabase')

<a name='Medallion.Threading.Redis.RedisDistributedReaderWriterLock.RedisDistributedReaderWriterLock(string,IDatabase,System.Action_Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder_).options'></a>

`options` [System.Action&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')[RedisDistributedSynchronizationOptionsBuilder](RedisDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Action-1 'System.Action`1')