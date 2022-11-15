using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.ZooKeeper;

using org.apache.zookeeper;

/// <summary>
/// A distributed reader-writer lock based on the ZooKeeper shared lock recipe (https://zookeeper.apache.org/doc/current/recipes.html).
/// </summary>
public sealed partial class ZooKeeperDistributedReaderWriterLock : IInternalDistributedReaderWriterLock<ZooKeeperDistributedReaderWriterLockHandle>
{
    private const string ReadNodePrefix = "read-",
        WriteNodePrefix = "write-";

    private readonly ZooKeeperSynchronizationHelper _synchronizationHelper;

    /// <summary>
    /// Constructs a new lock based on the provided <paramref name="path"/>, <paramref name="connectionString"/>, and <paramref name="options"/>.
    /// 
    /// If <paramref name="assumePathExists"/> is specified, then the node will not be created as part of acquiring nor will it be 
    /// deleted after releasing (defaults to false).
    /// </summary>
    public ZooKeeperDistributedReaderWriterLock(
        ZooKeeperPath path,
        string connectionString,
        bool assumePathExists = false,
        Action<ZooKeeperDistributedSynchronizationOptionsBuilder>? options = null)
        : this(path, assumePathExists: assumePathExists, connectionString, options)
    {
        if (path == default) { throw new ArgumentNullException(nameof(path)); }
        if (path == ZooKeeperPath.Root) { throw new ArgumentException("Cannot be the root", nameof(path)); }
    }

    /// <summary>
    /// Constructs a new lock based on the provided <paramref name="name"/>, <paramref name="connectionString"/>, and <paramref name="options"/>.
    /// 
    /// The lock's path will be a parent node of the root directory '/'. If <paramref name="name"/> is not a valid node name, it will be transformed to ensure
    /// validity.
    /// </summary>
    public ZooKeeperDistributedReaderWriterLock(string name, string connectionString, Action<ZooKeeperDistributedSynchronizationOptionsBuilder>? options = null)
        : this(ZooKeeperPath.Root, name, connectionString, options)
    {
    }

    /// <summary>
    /// Constructs a new lock based on the provided <paramref name="directoryPath"/>, <paramref name="name"/>, <paramref name="connectionString"/>, and <paramref name="options"/>.
    /// 
    /// The lock's path will be a parent node of <paramref name="directoryPath"/>. If <paramref name="name"/> is not a valid node name, it will be transformed to ensure
    /// validity.
    /// </summary>
    public ZooKeeperDistributedReaderWriterLock(ZooKeeperPath directoryPath, string name, string connectionString, Action<ZooKeeperDistributedSynchronizationOptionsBuilder>? options = null)
        : this(
              (directoryPath == default ? throw new ArgumentNullException(nameof(directoryPath)) : directoryPath).GetChildNodePathWithSafeName(name),
              assumePathExists: false,
              connectionString,
              options)
    {
    }

    private ZooKeeperDistributedReaderWriterLock(ZooKeeperPath nodePath, bool assumePathExists, string connectionString, Action<ZooKeeperDistributedSynchronizationOptionsBuilder>? optionsBuilder) =>
        this._synchronizationHelper = new ZooKeeperSynchronizationHelper(nodePath, assumePathExists, connectionString, optionsBuilder);

    /// <summary>
    /// The zookeeper node path
    /// </summary>
    public ZooKeeperPath Path => this._synchronizationHelper.Path;

    /// <summary>
    /// Implements <see cref="IDistributedReaderWriterLock.Name"/>. Implemented explicitly to avoid confusion with the fact
    /// that this will include the leading "/" and base directory alongside the passed-in name.
    /// </summary>
    string IDistributedReaderWriterLock.Name => this.Path.ToString();

    async ValueTask<ZooKeeperDistributedReaderWriterLockHandle?> IInternalDistributedReaderWriterLock<ZooKeeperDistributedReaderWriterLockHandle>.InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken, bool isWrite)
    {
        var nodeHandleTask = isWrite
            ? this._synchronizationHelper.TryAcquireAsync(HasAcquiredWriteLock, WaitForWriteLockAcquiredOrChange, timeout, cancellationToken, WriteNodePrefix, alternateNodePrefix: ReadNodePrefix)
            : this._synchronizationHelper.TryAcquireAsync(HasAcquiredReadLock, WaitForReadLockAcquiredOrChange, timeout, cancellationToken, ReadNodePrefix, alternateNodePrefix: WriteNodePrefix);
        // we're forced to use sync-over-async here because ZooKeeperNetEx doesn't have synchronous APIs
        var nodeHandle = await nodeHandleTask.AwaitSyncOverAsync().ConfigureAwait(false);

        return nodeHandle != null ? new ZooKeeperDistributedReaderWriterLockHandle(nodeHandle) : null;
    }

    private static bool HasAcquiredReadLock(ZooKeeperSynchronizationHelper.State state) =>
        // We have the read lock if there are no writers ahead of us
        state.SortedChildren.TakeWhile(t => t.Path != state.EphemeralNodePath).All(t => t.Prefix != WriteNodePrefix);

    private static async Task<bool> WaitForReadLockAcquiredOrChange(ZooKeeper zooKeeper, ZooKeeperSynchronizationHelper.State state, Watcher watcher)
    {
        var nextLowestWriteNode = state.SortedChildren.TakeWhile(t => t.Path != state.EphemeralNodePath)
            .Last(t => t.Prefix == WriteNodePrefix);
        // If the next lowest write node is already gone, then the wait is done. Otherwise, leave the watcher on that
        // node so that we'll be notified when it changes (we can't acquire the lock before then)
        return await zooKeeper.existsAsync(nextLowestWriteNode.Path, watcher).ConfigureAwait(false) == null;
    }

    private static bool HasAcquiredWriteLock(ZooKeeperSynchronizationHelper.State state) =>
        state.SortedChildren[0].Path == state.EphemeralNodePath;

    private async Task<bool> WaitForWriteLockAcquiredOrChange(ZooKeeper zooKeeper, ZooKeeperSynchronizationHelper.State state, Watcher watcher)
    {
        var ephemeralNodeIndex = Array.FindIndex(state.SortedChildren, t => t.Path == state.EphemeralNodePath);
        var nextLowestChildNode = state.SortedChildren[ephemeralNodeIndex - 1].Path;
        // If the next lowest child node is already gone, then the wait is done. Otherwise, leave the watcher on that
        // node so that we'll be notified when it changes (we can't acquire the lock before then)
        return await zooKeeper.existsAsync(nextLowestChildNode, watcher).ConfigureAwait(false) == null;
    }
}
