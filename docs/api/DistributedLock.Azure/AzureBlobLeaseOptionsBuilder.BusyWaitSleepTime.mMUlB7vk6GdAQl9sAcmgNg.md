#### [DistributedLock.Azure](README.md 'README')
### [Medallion.Threading.Azure](Medallion.Threading.Azure.md 'Medallion.Threading.Azure').[AzureBlobLeaseOptionsBuilder](AzureBlobLeaseOptionsBuilder.md 'Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder')

## AzureBlobLeaseOptionsBuilder.BusyWaitSleepTime(TimeSpan, TimeSpan) Method

Waiting to acquire a lease requires a busy wait that alternates acquire attempts and sleeps.  
This determines how much time is spent sleeping between attempts. Lower values will raise the  
volume of acquire requests under contention but will also raise the responsiveness (how long  
it takes a waiter to notice that a contended the lease has become available).  
  
Specifying a range of values allows the implementation to select an actual value in the range   
at random for each sleep. This helps avoid the case where two clients become "synchronized"  
in such a way that results in one client monopolizing the lease.  
  
The default is [250ms, 1s]

```csharp
public Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder BusyWaitSleepTime(System.TimeSpan min, System.TimeSpan max);
```
#### Parameters

<a name='Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder.BusyWaitSleepTime(System.TimeSpan,System.TimeSpan).min'></a>

`min` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

<a name='Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder.BusyWaitSleepTime(System.TimeSpan,System.TimeSpan).max'></a>

`max` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

#### Returns
[AzureBlobLeaseOptionsBuilder](AzureBlobLeaseOptionsBuilder.md 'Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder')