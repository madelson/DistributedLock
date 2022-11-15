using Medallion.Threading.Tests.ZooKeeper;
using Medallion.Threading.ZooKeeper;
using NUnit.Framework;
using org.apache.zookeeper;
using org.apache.zookeeper.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.ZooKeeper;

public abstract class ZooKeeperSynchronizationCoreTestCases<TLockProvider>
    where TLockProvider : TestingLockProvider<TestingZooKeeperSynchronizationStrategy>, new()
{
    private TLockProvider _provider = default!;

    [SetUp]
    public void SetUp() => this._provider = new TLockProvider();

    [TearDown]
    public void TearDown() => this._provider.Dispose();

    [Test]
    public async Task TestDoesNotAttemptToCreateOrDeleteExistingNode()
    {
        // This doesn't work because creating the lock attempts to acquire which will then fail initially. We could work around this by testing
        // for a different set of conditions in the multi-ticket case, but the extra coverage doesn't seem valuable (we still have coverage of single-ticket)
        if (IsMultiTicketSemaphoreProvider) { Assert.Pass("not supported"); }

        var path = new ZooKeeperPath($"/{this.GetType()}.{nameof(this.TestDoesNotAttemptToCreateOrDeleteExistingNode)} ({TargetFramework.Current})");
        using var connection = await ZooKeeperConnection.DefaultPool.ConnectAsync(
            new ZooKeeperConnectionInfo(ZooKeeperPorts.DefaultConnectionString, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30), new EquatableReadOnlyList<ZooKeeperAuthInfo>(Array.Empty<ZooKeeperAuthInfo>())),
            CancellationToken.None
        );

        // pre-clean up just in case
        try { await connection.ZooKeeper.deleteAsync(path.ToString()); }
        catch (KeeperException.NoNodeException) { }

        this._provider.Strategy.AssumeNodeExists = true;
        var @lock = this._provider.CreateLockWithExactName(path.ToString());

        Assert.That(
            Assert.ThrowsAsync<InvalidOperationException>(() => @lock.TryAcquireAsync().AsTask()).Message,
            Does.Contain("does not exist")
        );

        await connection.ZooKeeper.createAsync(path.ToString(), Array.Empty<byte>(), new List<ACL> { ZooKeeperNodeCreator.PublicAcl }, CreateMode.PERSISTENT);
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
        Assert.DoesNotThrowAsync(async () => await (await this._provider.CreateLockWithExactName(this._provider.GetSafeName(name)).AcquireAsync()).DisposeAsync());

    [Test]
    public void TestGetSafeNameWithControlCharacters() => this.TestGetSafeName("\u001f\u009F\uf8ff\ufff1");

    [Test]
    public async Task TestCustomAclAndAuth()
    {
        // This doesn't work because creating the lock causes the node to be created (from taking the other tickets)
        // and releasing the lock doesn't cause the node to be deleted (due to those other tickets).
        if (IsMultiTicketSemaphoreProvider) { Assert.Pass("not supported"); }

        const string Username = "username";
        const string Password = "secretPassword";

        var unauthenticatedLock = this._provider.CreateLock(string.Empty);

        this._provider.Strategy.Options = o => o.AddAccessControl("digest", GenerateDigestAclId(Username, Password), 0x1f)
            .AddAuthInfo("digest", Encoding.UTF8.GetBytes($"{Username}:{Password}"));
        var @lock = this._provider.CreateLock(string.Empty); 

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
        // This doesn't work because creating the lock causes the node to be created (from taking the other tickets)
        // and releasing the lock doesn't cause the node to be deleted (due to those other tickets).
        if (IsMultiTicketSemaphoreProvider) { Assert.Pass("not supported"); }

        const string Username = "username";
        const string Password = "xyz";

        // ACL is the right format but the wrong password (this can easily happen if you get the encoding wrong)
        this._provider.Strategy.Options = o => o.AddAccessControl("digest", $"{Username}:1eYGPn6j9+P9osACW8ob4HhZT+s=", 0x1f)
            .AddAuthInfo("digest", Encoding.UTF8.GetBytes($"{Username}:{Password}"));
        var invalidAclLock = this._provider.CreateLock(string.Empty);

        // pre-cleanup to make sure we will actually create the path 
        using var connection = await ZooKeeperConnection.DefaultPool.ConnectAsync(
            new ZooKeeperConnectionInfo(ZooKeeperPorts.DefaultConnectionString, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30), new EquatableReadOnlyList<ZooKeeperAuthInfo>(Array.Empty<ZooKeeperAuthInfo>())),
            CancellationToken.None
        );
        try { await connection.ZooKeeper.deleteAsync(invalidAclLock.Name); }
        catch (KeeperException.NoNodeException) { }

        Assert.ThrowsAsync<KeeperException.NoAuthException>(() => invalidAclLock.AcquireAsync().AsTask());

        Assert.IsNull(await connection.ZooKeeper.existsAsync(invalidAclLock.Name));

        this._provider.Strategy.Options = null;
        var validLock = this._provider.CreateLock(string.Empty);
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

        var @lock = this._provider.CreateLockWithExactName(directory.GetChildNodePathWithSafeName("qux").ToString());

        await using (await @lock.AcquireAsync())
        {
            Assert.IsNotNull(await connection.ZooKeeper.existsAsync(directory.ToString()));
        }

        Assert.IsNotNull(await connection.ZooKeeper.existsAsync(directory.ToString()), "directory still exists");
    }

    [Test]
    public async Task TestThrowsIfPathDeletedWhileWaiting()
    {
        var @lock = this._provider.CreateLock(string.Empty);

        // hold the lock
        await using var handle = await @lock.AcquireAsync();

        using var connection = await ZooKeeperConnection.DefaultPool.ConnectAsync(
            new ZooKeeperConnectionInfo(ZooKeeperPorts.DefaultConnectionString, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30), new EquatableReadOnlyList<ZooKeeperAuthInfo>(Array.Empty<ZooKeeperAuthInfo>())),
            CancellationToken.None
        );
        var initialChildren = await connection.ZooKeeper.getChildrenAsync(@lock.Name);

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
                var children = await connection.ZooKeeper.getChildrenAsync(@lock.Name);
                var newChild = children.Children.Except(initialChildren.Children).SingleOrDefault();
                if (newChild != null) { return $"{@lock.Name}/{newChild}"; }

                if (DateTime.UtcNow - start >= TimeSpan.FromSeconds(10)) { Assert.Fail("Timed out"); }

                await Task.Delay(5);
            }
        }
    }

    private static bool IsMultiTicketSemaphoreProvider => 
        typeof(TLockProvider) == typeof(TestingSemaphore5AsMutexProvider<TestingZooKeeperDistributedSemaphoreProvider, TestingZooKeeperSynchronizationStrategy>);
}
