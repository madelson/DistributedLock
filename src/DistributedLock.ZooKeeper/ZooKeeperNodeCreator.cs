namespace Medallion.Threading.ZooKeeper;

using org.apache.zookeeper;
using org.apache.zookeeper.data;
using System.Linq;

internal static class ZooKeeperNodeCreator
{
    /// <summary>
    /// See https://zookeeper.apache.org/doc/r3.5.4-beta/zookeeperProgrammers.html under "Builtin ACL Schemes"
    /// </summary>
    public static readonly ACL PublicAcl = new(0x1f, new Id("world", "anyone"));

    public static async Task<string> CreateEphemeralSequentialNode(
        this ZooKeeperConnection connection,
        ZooKeeperPath directory, 
        string namePrefix, 
        IEnumerable<ACL> aclEnumerable,
        bool ensureDirectoryExists)
    {
        // If we are in charge of ensuring the directory, this algorithm loops until either we EnsureDirectory fails or we hit an error other than the directory
        // not existing. This supports concurrent node creation and directory creation as well as node creation and directory deletion.

        var acl = aclEnumerable.ToList();
        while (true)
        {
            var createdDirectories = ensureDirectoryExists
                ? await EnsureDirectoryAndGetCreatedPathsAsync().ConfigureAwait(false)
                : Array.Empty<ZooKeeperPath>();

            try
            {
                return await connection.ZooKeeper.createAsync($"{directory}{ZooKeeperPath.Separator}{namePrefix}", data: Array.Empty<byte>(), acl, CreateMode.EPHEMERAL_SEQUENTIAL).ConfigureAwait(false);
            }
            catch (KeeperException.NoNodeException ex)
            {
                // If we're not ensuring the directory, rethrow a more helpful error message. Otherwise,
                // swallow the error and go around the loop again
                if (!ensureDirectoryExists)
                {
                    throw new InvalidOperationException($"Node '{directory}' does not exist", ex);
                }
            }
            catch
            {
                // on an unhandled error, clean up any directories we created
                await TryCleanUpCreatedDirectoriesAsync(createdDirectories).ConfigureAwait(false);

                throw;
            }
        }

        async Task<IReadOnlyList<ZooKeeperPath>> EnsureDirectoryAndGetCreatedPathsAsync()
        {
            // This algorithm loops until either the directory exists or our creation attempt fails with something other
            // than NoNodeException or NodeExistsException. This supports concurrent directory creation as well as concurrent 
            // creation and deletion via optimistic concurrency

            var toCreate = new Stack<ZooKeeperPath>();
            toCreate.Push(directory);
            List<ZooKeeperPath>? created = null;
            do
            {
                var directoryToCreate = toCreate.Peek();
                if (directoryToCreate == ZooKeeperPath.Root)
                {
                    throw new InvalidOperationException($"Received {typeof(KeeperException.NoNodeException)} when creating child node of directory '{ZooKeeperPath.Root}'");
                }

                try
                {
                    await connection.ZooKeeper.createAsync(directoryToCreate.ToString(), data: Array.Empty<byte>(), acl, CreateMode.PERSISTENT).ConfigureAwait(false);
                    toCreate.Pop();
                    (created ??= new List<ZooKeeperPath>()).Add(directoryToCreate);
                }
                catch (KeeperException.NodeExistsException) // someone else created it
                {
                    toCreate.Pop();
                }
                catch (KeeperException.NoNodeException) // parent needs to be created
                {
                    toCreate.Push(directoryToCreate.GetDirectory()!.Value);
                }
                catch
                {
                    // on an unhandled failure, attempt to clean up our work
                    if (created != null) { await TryCleanUpCreatedDirectoriesAsync(created).ConfigureAwait(false); }
                    
                    throw;
                }
            }
            while (toCreate.Count != 0);

            return created ?? (IReadOnlyList<ZooKeeperPath>)Array.Empty<ZooKeeperPath>();
        }

        async Task TryCleanUpCreatedDirectoriesAsync(IReadOnlyList<ZooKeeperPath> createdDirectories)
        {
            try
            {
                // delete in reverse order of creation
                for (var i = createdDirectories.Count - 1; i >= 0; --i)
                {
                    await connection.ZooKeeper.deleteAsync(createdDirectories[i].ToString()).ConfigureAwait(false);
                }
            }
            catch
            {
                // swallow errors, since there's a good chance this cleanup fails the same way that the creation did
            }
        }
    }
}
