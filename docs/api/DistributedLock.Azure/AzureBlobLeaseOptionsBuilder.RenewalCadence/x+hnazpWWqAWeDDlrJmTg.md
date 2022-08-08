#### [DistributedLock.Azure](README.md 'README')
### [Medallion.Threading.Azure](Medallion.Threading.Azure.md 'Medallion.Threading.Azure').[AzureBlobLeaseOptionsBuilder](AzureBlobLeaseOptionsBuilder.md 'Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder')

## AzureBlobLeaseOptionsBuilder.RenewalCadence(TimeSpan) Method

Determines how frequently the lease will be renewed when held. More frequent renewal means more unnecessary requests
but also a lower chance of losing the lease due to the process hanging or otherwise failing to get its renewal request in
before the lease duration expires.

To disable auto-renewal, specify [System.Threading.Timeout.InfiniteTimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.Timeout.InfiniteTimeSpan 'System.Threading.Timeout.InfiniteTimeSpan')

Defaults to 1/3 of the specified lease duration (may be infinite).

```csharp
public Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder RenewalCadence(System.TimeSpan renewalCadence);
```
#### Parameters

<a name='Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder.RenewalCadence(System.TimeSpan).renewalCadence'></a>

`renewalCadence` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

#### Returns
[AzureBlobLeaseOptionsBuilder](AzureBlobLeaseOptionsBuilder.md 'Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder')