#### [DistributedLock.WaitHandles](README.md 'README')
### [Medallion.Threading.WaitHandles](Medallion.Threading.WaitHandles.md 'Medallion.Threading.WaitHandles').[EventWaitHandleDistributedLock](EventWaitHandleDistributedLock.md 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock')

## EventWaitHandleDistributedLock(string, Nullable<TimeSpan>, bool) Constructor

Constructs a lock with the given [name](EventWaitHandleDistributedLock..ctor.2Tva732RJcbYY7yOGc2Dtg.md#Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.EventWaitHandleDistributedLock(string,System.Nullable_System.TimeSpan_,bool).name 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.EventWaitHandleDistributedLock(string, System.Nullable<System.TimeSpan>, bool).name').

[abandonmentCheckCadence](EventWaitHandleDistributedLock..ctor.2Tva732RJcbYY7yOGc2Dtg.md#Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.EventWaitHandleDistributedLock(string,System.Nullable_System.TimeSpan_,bool).abandonmentCheckCadence 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.EventWaitHandleDistributedLock(string, System.Nullable<System.TimeSpan>, bool).abandonmentCheckCadence') specifies how frequently we refresh our [System.Threading.EventWaitHandle](https://docs.microsoft.com/en-us/dotnet/api/System.Threading.EventWaitHandle 'System.Threading.EventWaitHandle') object in case it is abandoned by
its original owner. The default is 2s.

Unless [exactName](EventWaitHandleDistributedLock..ctor.2Tva732RJcbYY7yOGc2Dtg.md#Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.EventWaitHandleDistributedLock(string,System.Nullable_System.TimeSpan_,bool).exactName 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.EventWaitHandleDistributedLock(string, System.Nullable<System.TimeSpan>, bool).exactName') is specified, [name](EventWaitHandleDistributedLock..ctor.2Tva732RJcbYY7yOGc2Dtg.md#Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.EventWaitHandleDistributedLock(string,System.Nullable_System.TimeSpan_,bool).name 'Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.EventWaitHandleDistributedLock(string, System.Nullable<System.TimeSpan>, bool).name') will be escaped/hashed to ensure name validity.

```csharp
public EventWaitHandleDistributedLock(string name, System.Nullable<System.TimeSpan> abandonmentCheckCadence=null, bool exactName=false);
```
#### Parameters

<a name='Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.EventWaitHandleDistributedLock(string,System.Nullable_System.TimeSpan_,bool).name'></a>

`name` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.EventWaitHandleDistributedLock(string,System.Nullable_System.TimeSpan_,bool).abandonmentCheckCadence'></a>

`abandonmentCheckCadence` [System.Nullable&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')[System.TimeSpan](https://docs.microsoft.com/en-us/dotnet/api/System.TimeSpan 'System.TimeSpan')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Nullable-1 'System.Nullable`1')

<a name='Medallion.Threading.WaitHandles.EventWaitHandleDistributedLock.EventWaitHandleDistributedLock(string,System.Nullable_System.TimeSpan_,bool).exactName'></a>

`exactName` [System.Boolean](https://docs.microsoft.com/en-us/dotnet/api/System.Boolean 'System.Boolean')