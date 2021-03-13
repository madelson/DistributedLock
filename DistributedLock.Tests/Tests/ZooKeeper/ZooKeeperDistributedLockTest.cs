using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading.Internal;
using Medallion.Threading.ZooKeeper;
using NUnit.Framework;
using org.apache.zookeeper;
using org.apache.zookeeper.data;

namespace Medallion.Threading.Tests.ZooKeeper
{
    // todo many of these can be in an abstract test instead
    public class ZooKeeperDistributedLockTest
    {
        [Test]
        public void TestValidatesConstructorArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedLock(null!, ZooKeeperPorts.DefaultConnectionString));
            Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedLock("name", null!));
            Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedLock(default(ZooKeeperPath), ZooKeeperPorts.DefaultConnectionString));
            Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedLock(new ZooKeeperPath("/name"), null!));
            Assert.Throws<ArgumentException>(() => new ZooKeeperDistributedLock(ZooKeeperPath.Root, ZooKeeperPorts.DefaultConnectionString));
            Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedLock(default(ZooKeeperPath), "name", ZooKeeperPorts.DefaultConnectionString));
            Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedLock(new ZooKeeperPath("/dir"), null!, ZooKeeperPorts.DefaultConnectionString));
            Assert.Throws<ArgumentNullException>(() => new ZooKeeperDistributedLock(new ZooKeeperPath("/dir"), "name", default(string)!));
        }

        [Test]
        public void TestNameReturnsPathString()
        {
            var @lock = new ZooKeeperDistributedLock("some/crazy/name", ZooKeeperPorts.DefaultConnectionString);
            @lock.As<IDistributedLock>().Name.ShouldEqual(@lock.Path.ToString());
        }

        [Test]
        public void TestProperlyCombinesDirectoryAndName()
        {
            new ZooKeeperDistributedLock(new ZooKeeperPath("/dir"), "a", ZooKeeperPorts.DefaultConnectionString).Path.ToString().ShouldEqual("/dir/a");
            Assert.That(new ZooKeeperDistributedLock(new ZooKeeperPath("/a/b"), "c/d", ZooKeeperPorts.DefaultConnectionString).Path.ToString(), Does.StartWith("/a/b/"));
        }

        [Test]
        public async Task TestDoesNotAttemptToCreateOrDeleteExistingNode()
        {
            var path = new ZooKeeperPath($"/{this.GetType()}.{nameof(this.TestDoesNotAttemptToCreateOrDeleteExistingNode)} ({TargetFramework.Current})");
            using var connection = await ZooKeeperConnection.DefaultPool.ConnectAsync(
                new ZooKeeperConnectionInfo(ZooKeeperPorts.DefaultConnectionString, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30), new EquatableReadOnlyList<ZooKeeperAuthInfo>(Array.Empty<ZooKeeperAuthInfo>())),
                CancellationToken.None
            );

            // pre-clean up just in case
            try { await connection.ZooKeeper.deleteAsync(path.ToString()); }
            catch (KeeperException.NoNodeException) { } 

            var @lock = new ZooKeeperDistributedLock(path, ZooKeeperPorts.DefaultConnectionString);

            Assert.That(
                Assert.ThrowsAsync<InvalidOperationException>(() => @lock.TryAcquireAsync().AsTask()).Message,
                Does.Contain("does not exist")
            );

            await connection.ZooKeeper.createAsync(path.ToString(), Array.Empty<byte>(), new List<ACL> { ZooKeeperHelper.PublicAcl }, CreateMode.PERSISTENT);
            try
            {
                await using (var handle = await @lock.TryAcquireAsync())
                {
                    Assert.IsNotNull(handle);
                }

                Assert.IsNotNull(await connection.ZooKeeper.existsAsync(path.ToString()));
            }
            finally
            {
                await connection.ZooKeeper.deleteAsync(path.ToString());
            }
        }

        [TestCase("/")]
        [TestCase(".")]
        [TestCase("..")]
        [TestCase("zookeeper")]
        [TestCase("abc\0")]
        public void TestGetSafeName(string name) =>
            Assert.DoesNotThrowAsync(async () => await (await new ZooKeeperDistributedLock(name, ZooKeeperPorts.DefaultConnectionString).AcquireAsync()).DisposeAsync());

        [Test]
        public void TestGetSafeNameWithControlCharacters() => this.TestGetSafeName("\u001f\u009F\uf8ff\ufff1");

        [Test]
        public async Task TestCustomAclAndAuth()
        {
            const string Username = "username";
            const string Password = "secretPassword";
            
            var lockName = TestHelper.UniqueName;
            var @lock = new ZooKeeperDistributedLock(
                lockName,
                ZooKeeperPorts.DefaultConnectionString,
                options: o => o.AddAccessControl("digest", GenerateDigestAclId(Username, Password), 0x1f)
                    .AddAuthInfo("digest", Encoding.UTF8.GetBytes($"{Username}:{Password}"))
            );

            var unauthenticatedLock = new ZooKeeperDistributedLock(lockName, ZooKeeperPorts.DefaultConnectionString);
            
            await using (await @lock.AcquireAsync())
            {
                Assert.ThrowsAsync<KeeperException.NoAuthException>(() => unauthenticatedLock.TryAcquireAsync().AsTask());
            }

            Assert.DoesNotThrowAsync(async () => await (await unauthenticatedLock.AcquireAsync()).DisposeAsync());

            // Based on 
            // https://github.com/apache/zookeeper/blob/d8561f620fa8611e9a6819d9879b0f18e5a404a9/zookeeper-server/src/main/java/org/apache/zookeeper/server/auth/DigestAuthenticationProvider.java
            static string GenerateDigestAclId(string username, string password)
            {
                using var sha = SHA1.Create();
                var digest = sha.ComputeHash(Encoding.UTF8.GetBytes($"{username}:{password}"));
                return $"{username}:{Convert.ToBase64String(digest)}";
            }
        }

        [Test]
        public async Task TestInvalidAclDoesNotCorruptStore()
        {
            const string Username = "username";
            const string Password = "xyz";

            var lockName = TestHelper.UniqueName;
            var invalidAclLock = new ZooKeeperDistributedLock(
                lockName,
                ZooKeeperPorts.DefaultConnectionString,
                // ACL is the right format but the wrong password (this can easily happen if you get the encoding wrong)
                options: o => o.AddAccessControl("digest", $"{Username}:1eYGPn6j9+P9osACW8ob4HhZT+s=", 0x1f)
                    .AddAuthInfo("digest", Encoding.UTF8.GetBytes($"{Username}:{Password}"))
            );

            // pre-cleanup to make sure we will actually create the path 
            using var connection = await ZooKeeperConnection.DefaultPool.ConnectAsync(
                new ZooKeeperConnectionInfo(ZooKeeperPorts.DefaultConnectionString, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30), new EquatableReadOnlyList<ZooKeeperAuthInfo>(Array.Empty<ZooKeeperAuthInfo>())),
                CancellationToken.None
            );
            try { await connection.ZooKeeper.deleteAsync(invalidAclLock.Path.ToString()); }
            catch (KeeperException.NoNodeException) { }

            Assert.ThrowsAsync<KeeperException.NoAuthException>(() => invalidAclLock.AcquireAsync().AsTask());

            Assert.IsNull(await connection.ZooKeeper.existsAsync(invalidAclLock.Path.ToString()));

            var validLock = new ZooKeeperDistributedLock(lockName, ZooKeeperPorts.DefaultConnectionString);
            Assert.DoesNotThrowAsync(async () => await (await validLock.AcquireAsync()).DisposeAsync());
        }

        [Test]
        public async Task TestDeepDirectoryCreation()
        {
            var directory = new ZooKeeperPath($"/{TestHelper.UniqueName}/foo/bar/baz");

            // pre-cleanup to make sure we will actually create the directory 
            using var connection = await ZooKeeperConnection.DefaultPool.ConnectAsync(
                new ZooKeeperConnectionInfo(ZooKeeperPorts.DefaultConnectionString, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30), new EquatableReadOnlyList<ZooKeeperAuthInfo>(Array.Empty<ZooKeeperAuthInfo>())),
                CancellationToken.None
            );
            for (var toDelete = directory; toDelete != ZooKeeperPath.Root; toDelete = toDelete.GetDirectory()!.Value)
            {
                try { await connection.ZooKeeper.deleteAsync(toDelete.ToString()); }
                catch (KeeperException.NoNodeException) { }
            }

            var @lock = new ZooKeeperDistributedLock(directory, "qux", ZooKeeperPorts.DefaultConnectionString);

            await using (await @lock.AcquireAsync())
            {
                Assert.IsNotNull(await connection.ZooKeeper.existsAsync(directory.ToString()));
            }

            Assert.IsNotNull(await connection.ZooKeeper.existsAsync(directory.ToString()), "directory still exists");
        }

        [Test]
        public async Task TestThrowsIfPathDeletedWhileWaiting()
        {
            var @lock = new ZooKeeperDistributedLock(TestHelper.UniqueName, ZooKeeperPorts.DefaultConnectionString);

            // hold the lock
            await using var handle = await @lock.AcquireAsync();

            using var connection = await ZooKeeperConnection.DefaultPool.ConnectAsync(
                new ZooKeeperConnectionInfo(ZooKeeperPorts.DefaultConnectionString, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30), new EquatableReadOnlyList<ZooKeeperAuthInfo>(Array.Empty<ZooKeeperAuthInfo>())),
                CancellationToken.None
            );
            var initialChildren = await connection.ZooKeeper.getChildrenAsync(@lock.Path.ToString());

            // start waiting
            var blockedAcquireTask = @lock.AcquireAsync(TimeSpan.FromSeconds(30)).AsTask();
            // once the wait has started...
            var newChild = await WaitForNewChildAsync();
            // ... start another waiter...
            var blockedAcquireTask2 = @lock.AcquireAsync(TimeSpan.FromSeconds(30)).AsTask();
            // ... and delete the first waiter's node
            await connection.ZooKeeper.deleteAsync(newChild);

            // release the lock
            await handle.DisposeAsync();

            // the first waiter should throw
            Assert.ThrowsAsync<InvalidOperationException>(() => blockedAcquireTask);

            // the second waiter should complete
            Assert.DoesNotThrowAsync(async () => await (await blockedAcquireTask2).DisposeAsync());

            async Task<string> WaitForNewChildAsync()
            {
                var start = DateTime.UtcNow;
                while (true)
                {
                    var children = await connection.ZooKeeper.getChildrenAsync(@lock.Path.ToString());
                    var newChild = children.Children.Except(initialChildren.Children).SingleOrDefault();
                    if (newChild != null) { return $"{@lock.Path}/{newChild}"; }

                    if (DateTime.UtcNow - start >= TimeSpan.FromSeconds(10)) { Assert.Fail("Timed out"); }

                    await Task.Delay(5);
                }
            }
        }
    }
}
