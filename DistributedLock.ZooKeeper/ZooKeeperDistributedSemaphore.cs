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
    /// An implementation of <see cref="IDistributedSemaphore"/> based on ZooKeeper. Uses an approach similar to <see cref="ZooKeeperDistributedLock"/>.
    /// </summary>
    public sealed partial class ZooKeeperDistributedSemaphore : IInternalDistributedSemaphore<ZooKeeperDistributedSemaphoreHandle>
    {
        private readonly ZooKeeperSynchronizationHelper _synchronizationHelper;

        public ZooKeeperDistributedSemaphore(
            ZooKeeperPath path,
            int maxCount,
            string connectionString,
            bool assumeNodeExists = false,
            Action<ZooKeeperDistributedSynchronizationOptionsBuilder>? options = null)
            : this(path, maxCount, assumeNodeExists: assumeNodeExists, connectionString, options)
        {
            if (path == default) { throw new ArgumentNullException(nameof(path)); }
            if (path == ZooKeeperPath.Root) { throw new ArgumentException("Cannot be the root", nameof(path)); }
        }

        public ZooKeeperDistributedSemaphore(string name, int maxCount, string connectionString, Action<ZooKeeperDistributedSynchronizationOptionsBuilder>? options = null)
            : this(ZooKeeperPath.Root, name, maxCount, connectionString, options)
        {
        }

        public ZooKeeperDistributedSemaphore(ZooKeeperPath directoryPath, string name, int maxCount, string connectionString, Action<ZooKeeperDistributedSynchronizationOptionsBuilder>? options = null)
            : this(
                  (directoryPath == default ? throw new ArgumentNullException(nameof(directoryPath)) : directoryPath).GetChildNodePathWithSafeName(name),
                  maxCount,
                  assumeNodeExists: false,
                  connectionString,
                  options)
        {
        }

        private ZooKeeperDistributedSemaphore(ZooKeeperPath nodePath, int maxCount, bool assumeNodeExists, string connectionString, Action<ZooKeeperDistributedSynchronizationOptionsBuilder>? optionsBuilder)
        {
            if (maxCount < 1) { throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, "must be positive"); }
            this.MaxCount = maxCount;
            // setAcquiredMarker is needed because we use data changes as part of our wait procedure below
            this._synchronizationHelper = new ZooKeeperSynchronizationHelper(nodePath, assumeNodeExists, connectionString, optionsBuilder, setAcquiredMarker: true);
        }

        /// <summary>
        /// The zookeeper node path
        /// </summary>
        public ZooKeeperPath Path => this._synchronizationHelper.Path;

        /// <summary>
        /// Implements <see cref="IDistributedSemaphore.Name"/>. Implemented explicitly to avoid confusion with the fact
        /// that this will include the leading "/" and base directory alongside the passed-in name.
        /// </summary>
        string IDistributedSemaphore.Name => this.Path.ToString();

        public int MaxCount { get; }

        async ValueTask<ZooKeeperDistributedSemaphoreHandle?> IInternalDistributedSemaphore<ZooKeeperDistributedSemaphoreHandle>.InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken)
        {
            var nodeHandle = await this._synchronizationHelper.TryAcquireAsync(
                    hasAcquired: state => Array.FindIndex(state.SortedChildren, t => t.Path == state.EphemeralNodePath) < this.MaxCount,
                    waitAsync: async (zooKeeper, state, watcher) =>
                    {
                        var ephemeralNodeIndex = Array.FindIndex(state.SortedChildren, t => t.Path == state.EphemeralNodePath);
                        Invariant.Require(ephemeralNodeIndex >= this.MaxCount);

                        // if we're the next node in line for a ticket, wait for any changes in the collection of children
                        if (ephemeralNodeIndex == this.MaxCount)
                        {
                            var childNames = new HashSet<string>((await zooKeeper.getChildrenAsync(this.Path.ToString(), watcher).ConfigureAwait(false)).Children);
                            // If any of the children in front of us are missing, then the wait is done. Otherwise,
                            // let the watcher notify us when there is any change to the set of children
                            return state.SortedChildren.Take(ephemeralNodeIndex)
                                .Any(t => !childNames.Contains(t.Path.Substring(t.Path.LastIndexOf(ZooKeeperPath.Separator) + 1)));
                        }

                        // Otherwise, we just watch for the node ahead of us in line to have its data changed to the acquired marker. While we could
                        // watch all children in this case as well, that approach is less efficient because it will generate a herd effect where each
                        // new waiter or released waiter wakes up everyone else.
                        var nextLowestChildData = await zooKeeper.getDataAsync(state.SortedChildren[ephemeralNodeIndex - 1].Path, watcher).ConfigureAwait(false);
                        // If it's already acquired, then the wait is done. Otherwise, the watcher will notify us on any data change or on deletion of that node
                        return nextLowestChildData.Data.SequenceEqual(ZooKeeperSynchronizationHelper.AcquiredMarker);
                    },
                    timeout,
                    cancellationToken,
                    nodePrefix: "semaphore-"
                )
                // we're forced to use sync-over-async here because ZooKeeperNetEx doesn't have synchronous APIs
                .AwaitSyncOverAsync()
                .ConfigureAwait(false);

            return nodeHandle != null ? new ZooKeeperDistributedSemaphoreHandle(nodeHandle) : null;
        }
    }
}
