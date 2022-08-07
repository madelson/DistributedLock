#### [DistributedLock.ZooKeeper](README.md 'README')
### [Medallion.Threading.ZooKeeper](Medallion.Threading.ZooKeeper.md 'Medallion.Threading.ZooKeeper').[ZooKeeperDistributedSynchronizationOptionsBuilder](ZooKeeperDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder')

## ZooKeeperDistributedSynchronizationOptionsBuilder.AddAuthInfo(string, IReadOnlyList<byte>) Method

Specifies authentication info to be added to the Zookeeper connection with [org.apache.zookeeper.ZooKeeper.addAuthInfo(System.String,System.Byte[])](https://docs.microsoft.com/en-us/dotnet/api/org.apache.zookeeper.ZooKeeper.addAuthInfo#org_apache_zookeeper_ZooKeeper_addAuthInfo_System_String,System_Byte[]_ 'org.apache.zookeeper.ZooKeeper.addAuthInfo(System.String,System.Byte[])'). Each call  
to this method adds another entry to the list of auth info. See https://zookeeper.apache.org/doc/r3.5.4-beta/zookeeperProgrammers.html for more  
information on ZooKeeper auth.  
  
By default, no auth info is added.

```csharp
public Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder AddAuthInfo(string scheme, System.Collections.Generic.IReadOnlyList<byte> auth);
```
#### Parameters

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder.AddAuthInfo(string,System.Collections.Generic.IReadOnlyList_byte_).scheme'></a>

`scheme` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder.AddAuthInfo(string,System.Collections.Generic.IReadOnlyList_byte_).auth'></a>

`auth` [System.Collections.Generic.IReadOnlyList&lt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.IReadOnlyList-1 'System.Collections.Generic.IReadOnlyList`1')[System.Byte](https://docs.microsoft.com/en-us/dotnet/api/System.Byte 'System.Byte')[&gt;](https://docs.microsoft.com/en-us/dotnet/api/System.Collections.Generic.IReadOnlyList-1 'System.Collections.Generic.IReadOnlyList`1')

#### Returns
[ZooKeeperDistributedSynchronizationOptionsBuilder](ZooKeeperDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder')