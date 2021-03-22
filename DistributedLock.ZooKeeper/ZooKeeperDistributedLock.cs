using Medallion.Threading.Internal;
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
        private readonly ZooKeeperSynchronizationHelper _synchronizationHelper;

        public ZooKeeperDistributedLock(
            ZooKeeperPath path, 
            string connectionString, 
            bool assumeNodeExists = false,
            Action<ZooKeeperDistributedSynchronizationOptionsBuilder>? options = null)
            : this(path, assumeNodeExists: assumeNodeExists, connectionString, options)
        {
            if (path == default) { throw new ArgumentNullException(nameof(path)); }
            if (path == ZooKeeperPath.Root) { throw new ArgumentException("Cannot be the root", nameof(path)); }
        }

        public ZooKeeperDistributedLock(string name, string connectionString, Action<ZooKeeperDistributedSynchronizationOptionsBuilder>? options = null)
            : this(ZooKeeperPath.Root, name, connectionString, options)
        {
        }

        public ZooKeeperDistributedLock(ZooKeeperPath directoryPath, string name, string connectionString, Action<ZooKeeperDistributedSynchronizationOptionsBuilder>? options = null)
            : this(
                  (directoryPath == default ? throw new ArgumentNullException(nameof(directoryPath)) : directoryPath).GetChildNodePathWithSafeName(name), 
                  assumeNodeExists: false, 
                  connectionString, 
                  options)
        {
        }

        private ZooKeeperDistributedLock(ZooKeeperPath nodePath, bool assumeNodeExists, string connectionString, Action<ZooKeeperDistributedSynchronizationOptionsBuilder>? optionsBuilder) =>
            this._synchronizationHelper = new ZooKeeperSynchronizationHelper(nodePath, assumeNodeExists, connectionString, optionsBuilder);

        /// <summary>
        /// The zookeeper node path
        /// </summary>
        public ZooKeeperPath Path => this._synchronizationHelper.Path;

        /// <summary>
        /// Implements <see cref="IDistributedLock.Name"/>. Implemented explicitly to avoid confusion with the fact
        /// that this will include the leading "/" and base directory alongside the passed-in name.
        /// </summary>
        string IDistributedLock.Name => this.Path.ToString();

        async ValueTask<ZooKeeperDistributedLockHandle?> IInternalDistributedLock<ZooKeeperDistributedLockHandle>.InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken)
        {
            var nodeHandle = await this._synchronizationHelper.TryAcquireAsync(
                    hasAcquired: state => state.SortedChildren[0].Path == state.EphemeralNodePath,
                    waitAsync: async (zooKeeper, state, watcher) =>
                    {
                        var ephemeralNodeIndex = Array.FindIndex(state.SortedChildren, t => t.Path == state.EphemeralNodePath);
                        var nextLowestChildNode = state.SortedChildren[ephemeralNodeIndex - 1].Path;
                        // If the next lowest child node is already gone, then the wait is done. Otherwise, leave the watcher on that
                        // node so that we'll be notified when it changes (we can't acquire the lock before then)
                        return await zooKeeper.existsAsync(nextLowestChildNode, watcher).ConfigureAwait(false) == null;
                    },
                    timeout,
                    cancellationToken,
                    nodePrefix: "lock-"
                )
                // we're forced to use sync-over-async here because ZooKeeperNetEx doesn't have synchronous APIs
                .AwaitSyncOverAsync()
                .ConfigureAwait(false);

            return nodeHandle != null ? new ZooKeeperDistributedLockHandle(nodeHandle) : null;
        }
    }
}
