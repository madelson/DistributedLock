#### [DistributedLock.Redis](README.md 'README')
### [Medallion.Threading.Redis](Medallion.Threading.Redis.md 'Medallion.Threading.Redis').[RedisDistributedSynchronizationOptionsBuilder](RedisDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder')

## RedisDistributedSynchronizationOptionsBuilder.ExtensionCadence(TimeSpan) Method

Determines how frequently the lock will be extended while held. More frequent extension means more unnecessary requests  
but also a lower chance of losing the lock due to the process hanging or otherwise failing to get its extension request in  
before the lock expiry elapses.  
  
Defaults to 1/3 of the specified [MinValidityTime(TimeSpan)](RedisDistributedSynchronizationOptionsBuilder.MinValidityTime.fMaY2fG5om4C0LuUfUZtNg.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder.MinValidityTime(System.TimeSpan)').

```csharp
public Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder ExtensionCadence(System.TimeSpan extensionCadence);
```
#### Parameters

<a name='Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder.ExtensionCadence(System.TimeSpan).extensionCadence'></a>

`extensionCadence` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

#### Returns
[RedisDistributedSynchronizationOptionsBuilder](RedisDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder')