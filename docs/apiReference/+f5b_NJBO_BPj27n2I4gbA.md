### [Medallion.Threading.WaitHandles](0cv6wmZCIva5FK3cOR8t5g.md 'Medallion.Threading.WaitHandles').[WaitHandleDistributedSynchronizationProvider](UsS2hO+xXgwbwvK7JBT0hA.md 'Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider')

## WaitHandleDistributedSynchronizationProvider.CreateLock(string, bool) Method

Creates a [EventWaitHandleDistributedLock](LMZafb40QNKPqmMgiWZRCw.md 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock') with the given [name](+f5b_NJBO_BPj27n2I4gbA.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateLock(string,bool).name 'Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateLock(string, bool).name'). Unless  
[exactName](+f5b_NJBO_BPj27n2I4gbA.md#Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateLock(string,bool).exactName 'Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateLock(string, bool).exactName') is specified, invalid wait handle names will be escaped/hashed.

```csharp
public Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock CreateLock(string name, bool exactName=false);
```
#### Parameters

<a name='Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateLock(string,bool).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.WaitHandles.WaitHandleDistributedSynchronizationProvider.CreateLock(string,bool).exactName'></a>

`exactName` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')

#### Returns
[EventWaitHandleDistributedLock](LMZafb40QNKPqmMgiWZRCw.md 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock')