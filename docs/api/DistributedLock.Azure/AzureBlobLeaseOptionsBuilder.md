#### [DistributedLock.Azure](README.md 'README')
### [Medallion.Threading.Azure](Medallion.Threading.Azure.md 'Medallion.Threading.Azure')

## AzureBlobLeaseOptionsBuilder Class

Specifies options for an Azure blob lease

```csharp
public sealed class AzureBlobLeaseOptionsBuilder
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; AzureBlobLeaseOptionsBuilder

| Methods | |
| :--- | :--- |
| [BusyWaitSleepTime(TimeSpan, TimeSpan)](AzureBlobLeaseOptionsBuilder.BusyWaitSleepTime.mMUlB7vk6GdAQl9sAcmgNg.md 'Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder.BusyWaitSleepTime(System.TimeSpan, System.TimeSpan)') | Waiting to acquire a lease requires a busy wait that alternates acquire attempts and sleeps.<br/>This determines how much time is spent sleeping between attempts. Lower values will raise the<br/>volume of acquire requests under contention but will also raise the responsiveness (how long<br/>it takes a waiter to notice that a contended the lease has become available).<br/><br/>Specifying a range of values allows the implementation to select an actual value in the range <br/>at random for each sleep. This helps avoid the case where two clients become "synchronized"<br/>in such a way that results in one client monopolizing the lease.<br/><br/>The default is [250ms, 1s] |
| [Duration(TimeSpan)](AzureBlobLeaseOptionsBuilder.Duration.QEP6QPTxOgzkkg0h39jM/g.md 'Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder.Duration(System.TimeSpan)') | Specifies how long the lease will last, absent auto-renewal.<br/><br/>If auto-renewal is enabled (the default), then a shorter duration means more frequent auto-renewal requests,<br/>while an infinite duration means no auto-renewal requests. Furthermore, if the lease-holding process were to<br/>exit without explicitly releasing, then duration determines how long other processes would need to wait in <br/>order to acquire the lease.<br/><br/>If auto-renewal is disabled, then duration determines how long the lease will be held.<br/><br/>Defaults to 30s. |
| [RenewalCadence(TimeSpan)](AzureBlobLeaseOptionsBuilder.RenewalCadence./x+hnazpWWqAWeDDlrJmTg.md 'Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder.RenewalCadence(System.TimeSpan)') | Determines how frequently the lease will be renewed when held. More frequent renewal means more unnecessary requests<br/>but also a lower chance of losing the lease due to the process hanging or otherwise failing to get its renewal request in<br/>before the lease duration expires.<br/><br/>To disable auto-renewal, specify [System.Threading.Timeout.InfiniteTimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Timeout.InfiniteTimeSpan 'System.Threading.Timeout.InfiniteTimeSpan')<br/><br/>Defaults to 1/3 of the specified lease duration (may be infinite). |
