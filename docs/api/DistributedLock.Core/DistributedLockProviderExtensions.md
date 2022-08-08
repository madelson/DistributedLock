#### [DistributedLock.Core](README.md 'README')
### [Medallion.Threading](Medallion.Threading.md 'Medallion.Threading')

## DistributedLockProviderExtensions Class

Productivity helper methods for [IDistributedLockProvider](IDistributedLockProvider.md 'Medallion.Threading.IDistributedLockProvider')

```csharp
public static class DistributedLockProviderExtensions
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; DistributedLockProviderExtensions

| Methods | |
| :--- | :--- |
| [AcquireLock(this IDistributedLockProvider, string, Nullable&lt;TimeSpan&gt;, CancellationToken)](DistributedLockProviderExtensions.AcquireLock.DMP8nUt70Bor8mi0Krb42A.md 'Medallion.Threading.DistributedLockProviderExtensions.AcquireLock(this Medallion.Threading.IDistributedLockProvider, string, System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Equivalent to calling [CreateLock(string)](IDistributedLockProvider.CreateLock.lcl3dolUp9eZyeUENeHU9w.md 'Medallion.Threading.IDistributedLockProvider.CreateLock(string)') and then [Acquire(Nullable&lt;TimeSpan&gt;, CancellationToken)](IDistributedLock.Acquire.Q+8FXimBZqUrDv5tTRw59w.md 'Medallion.Threading.IDistributedLock.Acquire(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)'). |
| [AcquireLockAsync(this IDistributedLockProvider, string, Nullable&lt;TimeSpan&gt;, CancellationToken)](DistributedLockProviderExtensions.AcquireLockAsync.+REikXPbHJ/q+uAX8BwEjg.md 'Medallion.Threading.DistributedLockProviderExtensions.AcquireLockAsync(this Medallion.Threading.IDistributedLockProvider, string, System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)') | Equivalent to calling [CreateLock(string)](IDistributedLockProvider.CreateLock.lcl3dolUp9eZyeUENeHU9w.md 'Medallion.Threading.IDistributedLockProvider.CreateLock(string)') and then [AcquireAsync(Nullable&lt;TimeSpan&gt;, CancellationToken)](IDistributedLock.AcquireAsync.0Lol7Hv58Kl+UVYSOI6IpQ.md 'Medallion.Threading.IDistributedLock.AcquireAsync(System.Nullable<System.TimeSpan>, System.Threading.CancellationToken)'). |
| [TryAcquireLock(this IDistributedLockProvider, string, TimeSpan, CancellationToken)](DistributedLockProviderExtensions.TryAcquireLock.1sWD0pz+gM4NV17JJV3RTw.md 'Medallion.Threading.DistributedLockProviderExtensions.TryAcquireLock(this Medallion.Threading.IDistributedLockProvider, string, System.TimeSpan, System.Threading.CancellationToken)') | Equivalent to calling [CreateLock(string)](IDistributedLockProvider.CreateLock.lcl3dolUp9eZyeUENeHU9w.md 'Medallion.Threading.IDistributedLockProvider.CreateLock(string)') and then [TryAcquire(TimeSpan, CancellationToken)](IDistributedLock.TryAcquire.GcM73KNvUAY5aoOOhgln1g.md 'Medallion.Threading.IDistributedLock.TryAcquire(System.TimeSpan, System.Threading.CancellationToken)'). |
| [TryAcquireLockAsync(this IDistributedLockProvider, string, TimeSpan, CancellationToken)](DistributedLockProviderExtensions.TryAcquireLockAsync.eBA2EKzaS4eJOnnXosupQg.md 'Medallion.Threading.DistributedLockProviderExtensions.TryAcquireLockAsync(this Medallion.Threading.IDistributedLockProvider, string, System.TimeSpan, System.Threading.CancellationToken)') | Equivalent to calling [CreateLock(string)](IDistributedLockProvider.CreateLock.lcl3dolUp9eZyeUENeHU9w.md 'Medallion.Threading.IDistributedLockProvider.CreateLock(string)') and then [TryAcquireAsync(TimeSpan, CancellationToken)](IDistributedLock.TryAcquireAsync.ZLhweq3GadK5OwGmTwruEQ.md 'Medallion.Threading.IDistributedLock.TryAcquireAsync(System.TimeSpan, System.Threading.CancellationToken)'). |
