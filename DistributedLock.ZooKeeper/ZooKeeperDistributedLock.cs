using Medallion.Threading.Internal;
using org.apache.zookeeper.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.ZooKeeper
{
    /// <summary>
    /// An implementation of <see cref="IDistributedLock"/> based on ZooKeeper. Uses the lock recipe described in
    /// https://zookeeper.apache.org/doc/r3.1.2/recipes.html
    /// </summary>
    public sealed partial class ZooKeeperDistributedLock : IInternalDistributedLock<ZooKeeperDistributedLockHandle>
    {
        private const string NodePrefix = "lock-";

        private readonly bool _assumeNodePathExists;
        private readonly ZooKeeperConnectionInfo _connectionInfo;
        private readonly IReadOnlyList<ACL> _acl;

        // todo revisit this API; should we make assumption of existance optional?
        public ZooKeeperDistributedLock(ZooKeeperPath existingNodePath, string connectionString, Action<ZooKeeperDistributedSynchronizationOptionsBuilder>? options = null)
            : this(existingNodePath, assumeNodePathExists: true, connectionString, options)
        {
            if (existingNodePath == default) { throw new ArgumentNullException(nameof(existingNodePath)); }
            if (existingNodePath == ZooKeeperPath.Root) { throw new ArgumentException("Cannot be the root", nameof(existingNodePath)); }
        }

        public ZooKeeperDistributedLock(string name, string connectionString, Action<ZooKeeperDistributedSynchronizationOptionsBuilder>? options = null)
            : this(ZooKeeperPath.Root, name, connectionString, options)
        {
        }

        public ZooKeeperDistributedLock(ZooKeeperPath directoryNodePath, string name, string connectionString, Action<ZooKeeperDistributedSynchronizationOptionsBuilder>? options = null)
            : this(
                  (directoryNodePath == default ? throw new ArgumentNullException(nameof(directoryNodePath)) : directoryNodePath).CreateChildNodeWithSafeName(name), 
                  assumeNodePathExists: false, 
                  connectionString, 
                  options)
        {
        }

        private ZooKeeperDistributedLock(ZooKeeperPath nodePath, bool assumeNodePathExists, string connectionString, Action<ZooKeeperDistributedSynchronizationOptionsBuilder>? optionsBuilder)
        {
            this.Path = nodePath;
            this._assumeNodePathExists = assumeNodePathExists;
            var options = ZooKeeperDistributedSynchronizationOptionsBuilder.GetOptions(optionsBuilder);
            this._connectionInfo = new ZooKeeperConnectionInfo(
                connectionString ?? throw new ArgumentNullException(nameof(connectionString)),
                ConnectTimeout: options.ConnectTimeout,
                SessionTimeout: options.SessionTimeout,
                AuthInfo: options.AuthInfo
            );
            this._acl = options.Acl;
        }

        /// <summary>
        /// The zookeeper node path
        /// </summary>
        public ZooKeeperPath Path { get; }

        /// <summary>
        /// Implements <see cref="IDistributedLock.Name"/>. Implemented explicitly to avoid confusion with the fact
        /// that this will include the leading "/" and base directory alongside the passed-in name.
        /// </summary>
        string IDistributedLock.Name => this.Path.ToString();

        ValueTask<ZooKeeperDistributedLockHandle?> IInternalDistributedLock<ZooKeeperDistributedLockHandle>.InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken)
        {
            // we're forced to use sync-over-async here because ZooKeeperNetEx doesn't have synchronous APIs
            var task = this.InternalTryAcquireHelperAsync(timeout, cancellationToken);
            return SyncViaAsync.IsSynchronous ? task.GetAwaiter().GetResult().AsValueTask() : task.AsValueTask();
        }

        private async Task<ZooKeeperDistributedLockHandle?> InternalTryAcquireHelperAsync(TimeoutValue timeout, CancellationToken cancellationToken)
        {
            var connection = await ZooKeeperConnection.DefaultPool.ConnectAsync(this._connectionInfo, cancellationToken).ConfigureAwait(false);
            ZooKeeperDistributedLockHandle? result = null;
            try
            {
                result = await this.InternalTryAcquireAsync(connection, timeout, cancellationToken).ConfigureAwait(false);
                return result;
            }
            finally
            {
                if (result == null) { connection.Dispose(); }
            }
        }

        private async Task<ZooKeeperDistributedLockHandle?> InternalTryAcquireAsync(ZooKeeperConnection connection, TimeoutValue timeout, CancellationToken cancellationToken)
        {
            // (1) Call create( ) with a pathname of "_locknode_/lock-" and the sequence and ephemeral flags set.
            var ephemeralNodePath = await connection.CreateEphemeralSequentialNode(this.Path, NodePrefix, this._acl, ensureDirectoryExists: !this._assumeNodePathExists).ConfigureAwait(false);
            CancellationTokenSource? timeoutSource = null;
            var acquired = false;
            var ephemeralNodeLost = false;
            try
            {
                while (true)
                {
                    // (2) Call getChildren( ) on the lock node without setting the watch flag (this is important to avoid the herd effect).
                    var children = await connection.ZooKeeper.getChildrenAsync(this.Path.ToString()).ConfigureAwait(false);

                    // (3) If the pathname created in step 1 has the lowest sequence number suffix, the client has the lock and the client exits the protocol.
                    var childrenAndSequenceNumbers = await ZooKeeperSequentialPathHelper.FilterAndSortAsync(
                        parentNode: this.Path.ToString(),
                        childrenNames: children.Children,
                        getNodeCreationTimeAsync: connection.GetNodeCreationTimeAsync,
                        prefix: NodePrefix
                    ).ConfigureAwait(false);
                    var ephemeralNodeIndex = Array.FindIndex(childrenAndSequenceNumbers, t => t.Path == ephemeralNodePath);
                    if (ephemeralNodeIndex == 0)
                    {
                        acquired = true;
                        return new ZooKeeperDistributedLockHandle(new ZooKeeperNodeHandle(connection, ephemeralNodePath, shouldDeleteParent: !this._assumeNodePathExists));
                    }
                    // Sanity check; this could happen if someone else deletes it out from under us. We must check
                    // this first because it covers the empty collection case
                    if (ephemeralNodeIndex < 0) 
                    {
                        ephemeralNodeLost = true;
                        throw new InvalidOperationException($"Node '{ephemeralNodePath}' was created, but no longer exists"); 
                    } 
                    if (timeout.IsZero) { return null; } // don't proceed further if we're not willing to wait

                    // (4) The client calls exists( ) with the watch flag set on the path in the lock directory with the next lowest sequence number.
                    // (5) If exists( ) returns false, go to step 2. Otherwise, wait for a notification for the pathname from the previous step before going to step 2.
                    var nextLowestChildNode = childrenAndSequenceNumbers[ephemeralNodeIndex - 1].Path;
                    if (!timeout.IsInfinite) { timeoutSource ??= new CancellationTokenSource(timeout.TimeSpan); }
                    if ((await connection.WaitForNotExistsOrChanged(nextLowestChildNode, cancellationToken, timeoutSource?.Token ?? CancellationToken.None).ConfigureAwait(false)) == false)
                    {
                        return null; // timed out
                    }
                }
            }
            finally
            {
                timeoutSource?.Dispose();

                // if we failed to acquire, clean up
                if (!acquired)
                {
                    if (!ephemeralNodeLost)
                    {
                        await connection.ZooKeeper.deleteAsync(ephemeralNodePath).ConfigureAwait(false);
                    }
                    if (!this._assumeNodePathExists)
                    {
                        // If the parent node should be cleaned up, try to do so. This attempt will almost certainly fail because
                        // someone else is holding the lock. However, we could have encountered a race condition where the other holder
                        // released right after we failed to acquire and our ephemeral node prevented them from deleting. Therefore, we
                        // fire and forget this deletion to cover that case without slowing us down
                        _ = connection.ZooKeeper.deleteAsync(this.Path.ToString());
                    }
                }
            }
        }
    }
}
