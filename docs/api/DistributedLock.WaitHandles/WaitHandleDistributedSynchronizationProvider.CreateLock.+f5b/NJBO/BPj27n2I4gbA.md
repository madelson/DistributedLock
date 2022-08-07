#### [DistributedLock.WaitHandles](README.md 'README')
### [Medallion.Threading.WaitHandles](Medallion.Threading.WaitHandles.md 'Medallion.Threading.WaitHandles').[WaitHandleDistributedSynchronizationProvider](WaitHandleDistributedSynchronizationProvider.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider')

## WaitHandleDistributedSynchronizationProvider.CreateLock(string, bool) Method

Creates a [EventWaitHandleDistributedLock](EventWaitHandleDistributedLock.md 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock') with the given [name](WaitHandleDistributedSynchronizationProvider.CreateLock.+f5b/NJBO/BPj27n2I4gbA.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateLock(string,bool).name 'Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateLock(string, bool).name'). Unless  
[exactName](WaitHandleDistributedSynchronizationProvider.CreateLock.+f5b/NJBO/BPj27n2I4gbA.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateLock(string,bool).exactName 'Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateLock(string, bool).exactName') is specified, invalid wait handle names will be escaped/hashed.

```csharp
public Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock CreateLock(string name, bool exactName=false);
```
#### Parameters

<a name='Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateLock(string,bool).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateLock(string,bool).exactName'></a>

`exactName` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')

#### Returns
[EventWaitHandleDistributedLock](EventWaitHandleDistributedLock.md 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock')