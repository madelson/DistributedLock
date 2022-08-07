#### [DistributedLock.Redis](README.md 'README')
### [Medallion.Threading.Redis](Medallion.Threading.Redis.md 'Medallion.Threading.Redis')

## RedisDistributedSynchronizationOptionsBuilder Class

Options for configuring a redis-based distributed synchronization algorithm

```csharp
public sealed class RedisDistributedSynchronizationOptionsBuilder
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; RedisDistributedSynchronizationOptionsBuilder

| Methods | |
| :--- | :--- |
| [BusyWaitSleepTime(TimeSpan, TimeSpan)](RedisDistributedSynchronizationOptionsBuilder.BusyWaitSleepTime.Z/k+WORQrlfNeKapMJREAw.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder.BusyWaitSleepTime(System.TimeSpan, System.TimeSpan)') | Waiting to acquire a lock requires a busy wait that alternates acquire attempts and sleeps.<br/>This determines how much time is spent sleeping between attempts. Lower values will raise the<br/>volume of acquire requests under contention but will also raise the responsiveness (how long<br/>it takes a waiter to notice that a contended the lock has become available).<br/><br/>Specifying a range of values allows the implementation to select an actual value in the range <br/>at random for each sleep. This helps avoid the case where two clients become "synchronized"<br/>in such a way that results in one client monopolizing the lock.<br/><br/>The default is [10ms, 800ms] |
| [Expiry(TimeSpan)](RedisDistributedSynchronizationOptionsBuilder.Expiry.Aq56pTIqK1xlhiCtghZSyQ.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder.Expiry(System.TimeSpan)') | Specifies how long the lock will last, absent auto-extension. Because auto-extension exists,<br/>this value generally will have little effect on program behavior. However, making the expiry longer means that<br/>auto-extension requests can occur less frequently, saving resources. On the other hand, when a lock is abandoned<br/>without explicit release (e. g. if the holding process crashes), the expiry determines how long other processes<br/>would need to wait in order to acquire it.<br/><br/>Defaults to 30s. |
| [ExtensionCadence(TimeSpan)](RedisDistributedSynchronizationOptionsBuilder.ExtensionCadence.cKv2f155zX3kr5YAmhrxcw.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder.ExtensionCadence(System.TimeSpan)') | Determines how frequently the lock will be extended while held. More frequent extension means more unnecessary requests<br/>but also a lower chance of losing the lock due to the process hanging or otherwise failing to get its extension request in<br/>before the lock expiry elapses.<br/><br/>Defaults to 1/3 of the specified [MinValidityTime(TimeSpan)](RedisDistributedSynchronizationOptionsBuilder.MinValidityTime.fMaY2fG5om4C0LuUfUZtNg.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder.MinValidityTime(System.TimeSpan)'). |
| [MinValidityTime(TimeSpan)](RedisDistributedSynchronizationOptionsBuilder.MinValidityTime.fMaY2fG5om4C0LuUfUZtNg.md 'Medallion.Threading.Redis.RedisDistributedSynchronizationOptionsBuilder.MinValidityTime(System.TimeSpan)') | The lock expiry determines how long the lock will be held without being extended. However, since it takes some amount<br/>of time to acquire the lock, we will not have all of expiry available upon acquisition.<br/><br/>This value sets a minimum amount which we'll be guaranteed to have left once acquisition completes.<br/><br/>Defaults to 90% of the specified lock expiry. |
