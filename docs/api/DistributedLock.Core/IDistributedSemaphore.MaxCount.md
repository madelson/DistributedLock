#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading').[IDistributedSemaphore](IDistributedSemaphore.md 'Medallion.Threading.IDistributedSemaphore')

## IDistributedSemaphore.MaxCount Property

The maximum number of "tickets" available for the semaphore (ie the number of processes which can acquire
the semaphore concurrently).

```csharp
int MaxCount { get; }
```

#### Property Value
[System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')