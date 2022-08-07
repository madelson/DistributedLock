#### [DistributedLock.ZooKeeper](README.md 'README')
### [Medallion.Threading.ZooKeeper](Medallion.Threading.ZooKeeper.md 'Medallion.Threading.ZooKeeper').[ZooKeeperDistributedSynchronizationOptionsBuilder](ZooKeeperDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder')

## ZooKeeperDistributedSynchronizationOptionsBuilder.AddAccessControl(string, string, int) Method

Configures the access control list (ACL) for any created ZooKeeper nodes. Each call to this method adds another entry to the access control  
list. See https://zookeeper.apache.org/doc/r3.5.4-beta/zookeeperProgrammers.html for more information on ZooKeeper ACLs.  
  
If no ACL entries are specified, the ACL used will be a singleton list that grants all permissions to (world, anyone).

```csharp
public Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder AddAccessControl(string scheme, string id, int permissionFlags);
```
#### Parameters

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder.AddAccessControl(string,string,int).scheme'></a>

`scheme` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder.AddAccessControl(string,string,int).id'></a>

`id` [System.String](https://docs.microsoft.com/en-us/dotnet/api/System.String 'System.String')

<a name='Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder.AddAccessControl(string,string,int).permissionFlags'></a>

`permissionFlags` [System.Int32](https://docs.microsoft.com/en-us/dotnet/api/System.Int32 'System.Int32')

#### Returns
[ZooKeeperDistributedSynchronizationOptionsBuilder](ZooKeeperDistributedSynchronizationOptionsBuilder.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder')