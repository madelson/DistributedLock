### [Medallion.Threading.WaitHandles](0cv6wmZCIva5FK3cOR8t5g.md 'Medallion.Threading.WaitHandles')

## WaitHandleDistributedSynchronizationProvider Class

Implements [Medallion.Threading.IDistributedLockProvider](https://docs.microsoft.com/en-us/dotnet/api/Medallion.Threading.IDistributedLockProvider 'Medallion.Threading.IDistributedLockProvider') for [EventWaitHandleDistributedLock](LMZafb40QNKPqmMgiWZRCw.md 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock')  
and [Medallion.Threading.IDistributedSemaphoreProvider](https://docs.microsoft.com/en-us/dotnet/api/Medallion.Threading.IDistributedSemaphoreProvider 'Medallion.Threading.IDistributedSemaphoreProvider') for [WaitHandleDistributedSemaphore](u++QKv1+Gje9fkJlbjceow.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore').

```csharp
public sealed class WaitHandleDistributedSynchronizationProvider :
Medallion.Threading.IDistributedLockProvider,
Medallion.Threading.IDistributedSemaphoreProvider
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; WaitHandleDistributedSynchronizationProvider

Implements [Medallion.Threading.IDistributedLockProvider](https://docs.microsoft.com/en-us/dotnet/api/Medallion.Threading.IDistributedLockProvider 'Medallion.Threading.IDistributedLockProvider'), [Medallion.Threading.IDistributedSemaphoreProvider](https://docs.microsoft.com/en-us/dotnet/api/Medallion.Threading.IDistributedSemaphoreProvider 'Medallion.Threading.IDistributedSemaphoreProvider')

| Constructors | |
| :--- | :--- |
| [WaitHandleDistributedSynchronizationProvider(Nullable&lt;TimeSpan&gt;)](e6xRwWsJppw_KT0BMNM9Fg.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.WaitHandleDistributedSynchronizationProvider(System.Nullable<System.TimeSpan>)') | Constructs a [WaitHandleDistributedSynchronizationProvider](UsS2hO+xXgwbwvK7JBT0hA.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider') using the provided [abandonmentCheckCadence](e6xRwWsJppw_KT0BMNM9Fg.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.WaitHandleDistributedSynchronizationProvider(System.Nullable_System.TimeSpan_).abandonmentCheckCadence 'Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.WaitHandleDistributedSynchronizationProvider(System.Nullable<System.TimeSpan>).abandonmentCheckCadence'). |

| Methods | |
| :--- | :--- |
| [CreateLock(string, bool)](+f5b_NJBO_BPj27n2I4gbA.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateLock(string, bool)') | Creates a [EventWaitHandleDistributedLock](LMZafb40QNKPqmMgiWZRCw.md 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock') with the given [name](+f5b_NJBO_BPj27n2I4gbA.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateLock(string,bool).name 'Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateLock(string, bool).name'). Unless<br/>[exactName](+f5b_NJBO_BPj27n2I4gbA.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateLock(string,bool).exactName 'Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateLock(string, bool).exactName') is specified, invalid wait handle names will be escaped/hashed. |
| [CreateSemaphore(string, int, bool)](lScE6Nm5quO4w5D+oiHGrA.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateSemaphore(string, int, bool)') | Creates a [WaitHandleDistributedSemaphore](u++QKv1+Gje9fkJlbjceow.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSemaphore') with the given [name](lScE6Nm5quO4w5D+oiHGrA.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateSemaphore(string,int,bool).name 'Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateSemaphore(string, int, bool).name')<br/>and [maxCount](lScE6Nm5quO4w5D+oiHGrA.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateSemaphore(string,int,bool).maxCount 'Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateSemaphore(string, int, bool).maxCount'). Unless [exactName](lScE6Nm5quO4w5D+oiHGrA.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateSemaphore(string,int,bool).exactName 'Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateSemaphore(string, int, bool).exactName') is specified, invalid wait <br/>handle names will be escaped/hashed. |
