#### [DistributedLock.Azure](README.md 'README')
### [Medallion.Threading.Azure](Medallion.Threading.Azure.md 'Medallion.Threading.Azure').[AzureBlobLeaseOptionsBuilder](AzureBlobLeaseOptionsBuilder.md 'Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder')

## AzureBlobLeaseOptionsBuilder.Duration(TimeSpan) Method

Specifies how long the lease will last, absent auto-renewal.

If auto-renewal is enabled (the default), then a shorter duration means more frequent auto-renewal requests,
while an infinite duration means no auto-renewal requests. Furthermore, if the lease-holding process were to
exit without explicitly releasing, then duration determines how long other processes would need to wait in 
order to acquire the lease.

If auto-renewal is disabled, then duration determines how long the lease will be held.

Defaults to 30s.

```csharp
public Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder Duration(System.TimeSpan duration);
```
#### Parameters

<a name='Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder.Duration(System.TimeSpan).duration'></a>

`duration` [System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')

#### Returns
[AzureBlobLeaseOptionsBuilder](AzureBlobLeaseOptionsBuilder.md 'Medallion.Threading.Azure.AzureBlobLeaseOptionsBuilder')