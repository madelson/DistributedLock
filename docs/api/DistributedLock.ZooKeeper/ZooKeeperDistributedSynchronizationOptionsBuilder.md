#### [DistributedLock.ZooKeeper](README.md 'README')
### [Medallion.Threading.ZooKeeper](Medallion.Threading.ZooKeeper.md 'Medallion.Threading.ZooKeeper')

## ZooKeeperDistributedSynchronizationOptionsBuilder Class

Options for configuring ZooKeeper-based synchronization primitives

```csharp
public sealed class ZooKeeperDistributedSynchronizationOptionsBuilder
```

Inheritance [System.Object](https://docs.microsoft.com/en-us/dotnet/api/System.Object 'System.Object') &#129106; ZooKeeperDistributedSynchronizationOptionsBuilder

| Methods | |
| :--- | :--- |
| [AddAccessControl(string, string, int)](ZooKeeperDistributedSynchronizationOptionsBuilder.AddAccessControl.tEJOZTy+rziKjj9R59jrIQ.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder.AddAccessControl(string, string, int)') | Configures the access control list (ACL) for any created ZooKeeper nodes. Each call to this method adds another entry to the access control<br/>list. See https://zookeeper.apache.org/doc/r3.5.4-beta/zookeeperProgrammers.html for more information on ZooKeeper ACLs.<br/><br/>If no ACL entries are specified, the ACL used will be a singleton list that grants all permissions to (world, anyone). |
| [AddAuthInfo(string, IReadOnlyList&lt;byte&gt;)](ZooKeeperDistributedSynchronizationOptionsBuilder.AddAuthInfo.JC63oXMe1MWcEJ2k3bfJ0g.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder.AddAuthInfo(string, System.Collections.Generic.IReadOnlyList<byte>)') | Specifies authentication info to be added to the Zookeeper connection with [org.apache.zookeeper.ZooKeeper.addAuthInfo(System.String,System.Byte[])](https://docs.microsoft.com/en-us/dotnet/api/org.apache.zookeeper.ZooKeeper.addAuthInfo#org_apache_zookeeper_ZooKeeper_addAuthInfo_System_String,System_Byte[]_ 'org.apache.zookeeper.ZooKeeper.addAuthInfo(System.String,System.Byte[])'). Each call<br/>to this method adds another entry to the list of auth info. See https://zookeeper.apache.org/doc/r3.5.4-beta/zookeeperProgrammers.html for more<br/>information on ZooKeeper auth.<br/><br/>By default, no auth info is added. |
| [ConnectTimeout(TimeSpan)](ZooKeeperDistributedSynchronizationOptionsBuilder.ConnectTimeout.I/ihabf4bAq6D8+832gAKA.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder.ConnectTimeout(System.TimeSpan)') | Configures how long to wait to establish a connection to ZooKeeper before failing with a [System.TimeoutException](https://docs.microsoft.com/en-us/dotnet/api/System.TimeoutException 'System.TimeoutException').<br/><br/>Defaults to 15s. |
| [SessionTimeout(TimeSpan)](ZooKeeperDistributedSynchronizationOptionsBuilder.SessionTimeout.a4s4suCBjQ12y3wfkImgaw.md 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder.SessionTimeout(System.TimeSpan)') | Configures the [sessionTimeout](ZooKeeperDistributedSynchronizationOptionsBuilder.SessionTimeout.a4s4suCBjQ12y3wfkImgaw.md#Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder.SessionTimeout(System.TimeSpan).sessionTimeout 'Medallion.Threading.ZooKeeper.ZooKeeperDistributedSynchronizationOptionsBuilder.SessionTimeout(System.TimeSpan).sessionTimeout') for connections to ZooKeeper. Because the underlying ZooKeeper client periodically renews<br/>the session, this value generally will not impact behavior. Lower values mean that locks will be released more quickly following a crash<br/>of the lock-holding process, but also increase the risk that transient connection issues will result in a dropped lock.<br/><br/>Defaults to 20s. |
