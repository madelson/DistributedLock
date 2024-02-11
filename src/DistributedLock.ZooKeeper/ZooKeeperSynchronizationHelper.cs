using Medallion.Threading.Internal;
using org.apache.zookeeper.data;
using System.Text;

namespace Medallion.Threading.ZooKeeper;

using org.apache.zookeeper;

/// <summary>
/// Provides a common structure for ZooKeeper synchronization algorithms. The structure is:
/// * Create a sequential ephemeral node
/// * Loop
///     * Compare our node's sequence number to its siblings to see if we've acquired
///     * If we haven't acquired, wait for a watcher to tell us that something has changed
/// </summary>
internal class ZooKeeperSynchronizationHelper
{
    public static readonly IReadOnlyList<byte> AcquiredMarker = Encoding.UTF8.GetBytes("ACQUIRED");

    private readonly ZooKeeperConnectionInfo _connectionInfo;
    private readonly IReadOnlyList<ACL> _acl;
    private readonly bool _assumePathExists, _setAcquiredMarker;
    
    public ZooKeeperSynchronizationHelper(
        ZooKeeperPath path, 
        bool assumePathExists, 
        string connectionString, 
        Action<ZooKeeperDistributedSynchronizationOptionsBuilder>? optionsBuilder,
        bool setAcquiredMarker = false)
    {
        this.Path = path;
        this._assumePathExists = assumePathExists;
        var options = ZooKeeperDistributedSynchronizationOptionsBuilder.GetOptions(optionsBuilder);
        this._connectionInfo = new ZooKeeperConnectionInfo(
            connectionString ?? throw new ArgumentNullException(nameof(connectionString)),
            ConnectTimeout: options.ConnectTimeout,
            SessionTimeout: options.SessionTimeout,
            AuthInfo: options.AuthInfo
        );
        this._acl = options.Acl;
        this._setAcquiredMarker = setAcquiredMarker;
    }

    public ZooKeeperPath Path { get; }

    public async Task<ZooKeeperNodeHandle?> TryAcquireAsync(
        Func<State, bool> hasAcquired,
        Func<ZooKeeper, State, Watcher, Task<bool>> waitAsync,
        TimeoutValue timeout, 
        CancellationToken cancellationToken,
        string nodePrefix,
        string? alternateNodePrefix = null)
    {
        var acquired = false;
        var ephemeralNodeLost = false;
        ZooKeeperConnection? connection = null;
        string? ephemeralNodePath = null;
        var timeoutSource = new CancellationTokenSource(timeout.TimeSpan);
        
        try
        {
            connection = await ZooKeeperConnection.DefaultPool.ConnectAsync(this._connectionInfo, cancellationToken).ConfigureAwait(false);
            
            // create a path that represents both our hold on the synch object and our place in line waiting for it
            ephemeralNodePath = await connection.CreateEphemeralSequentialNode(this.Path, nodePrefix, this._acl, ensureDirectoryExists: !this._assumePathExists).ConfigureAwait(false);

            while (true)
            {
                // get the children of the node, filter them by prefix, and sort them by age; we'll use this to determine our place in the list
                var children = await connection.ZooKeeper.getChildrenAsync(this.Path.ToString()).ConfigureAwait(false);
                var sortedChildren = await ZooKeeperSequentialPathHelper.FilterAndSortAsync(
                    parentNode: this.Path.ToString(),
                    childrenNames: children.Children,
                    getNodeCreationTimeAsync: connection.GetNodeCreationTimeAsync,
                    prefix: nodePrefix,
                    alternatePrefix: alternateNodePrefix
                ).ConfigureAwait(false);

                // Sanity check; this could happen if someone else deletes it out from under us. We must check
                // this first because it covers the empty collection case
                if (!sortedChildren.Any(t => t.Path == ephemeralNodePath))
                {
                    ephemeralNodeLost = true;
                    throw new InvalidOperationException($"Node '{ephemeralNodePath}' was created, but no longer exists");
                }

                // see if we've acquired
                var state = new State(ephemeralNodePath, sortedChildren);
                if (hasAcquired(state))
                {
                    if (this._setAcquiredMarker)
                    {
                        await connection.ZooKeeper.setDataAsync(ephemeralNodePath, AcquiredMarker.ToArray()).ConfigureAwait(false);
                    }
                    acquired = true;
                    return new ZooKeeperNodeHandle(connection, ephemeralNodePath, shouldDeleteParent: !this._assumePathExists);
                }

                // wait for something to change
                var waitCompletionSource = new TaskCompletionSource<bool>();
                using var timeoutRegistration = timeoutSource.Token.Register(state => ((TaskCompletionSource<bool>)state).TrySetResult(false), waitCompletionSource);
                using var cancellationRegistration = cancellationToken.Register(state => ((TaskCompletionSource<bool>)state).TrySetCanceled(), waitCompletionSource);
                // this is needed because if the connection goes down and never recovers, we'll never get the session expired notification
                using var connectionLostRegistration = connection.ConnectionLostToken.Register(
                    state => ((TaskCompletionSource<bool>)state).TrySetException(new InvalidOperationException("Lost connection to ZooKeeper")),
                    state: waitCompletionSource
                );
                if (!waitCompletionSource.Task.IsCompleted
                    && await waitAsync(connection.ZooKeeper, state, new WaitCompletionSourceWatcher(waitCompletionSource)).ConfigureAwait(false)) 
                {
                    waitCompletionSource.TrySetResult(true);
                }

                if (!await waitCompletionSource.Task.ConfigureAwait(false))
                {
                    return null; // wait timed out
                }
            }
        }
        finally
        {
            timeoutSource.Dispose();

            // if we failed to acquire, clean up the connection/node path
            if (!acquired)
            {
                await this.CleanUpOnFailureAsync(connection, ephemeralNodePath, ephemeralNodeLost).ConfigureAwait(false);
            }
        }
    }

    private async Task CleanUpOnFailureAsync(ZooKeeperConnection? connection, string? ephemeralNodePath, bool ephemeralNodeLost)
    {
        if (connection == null) { return; }

        try
        {
            if (ephemeralNodePath != null)
            {
                if (!ephemeralNodeLost)
                {
                    await connection.ZooKeeper.deleteAsync(ephemeralNodePath).ConfigureAwait(false);
                }
                if (!this._assumePathExists)
                {
                    // If the parent node should be cleaned up, try to do so. This attempt will almost certainly fail because
                    // someone else is holding the lock. However, we could have encountered a race condition where the other holder
                    // released right after we failed to acquire and our ephemeral node prevented them from deleting. Therefore, we
                    // fire and forget this deletion to cover that case without slowing us down
                    _ = connection.ZooKeeper.deleteAsync(this.Path.ToString());
                }
            }
        }
        finally
        {
            connection.Dispose();
        }
    }

    private sealed class WaitCompletionSourceWatcher : Watcher
    {
        private readonly TaskCompletionSource<bool> _waitCompletionSource;

        public WaitCompletionSourceWatcher(TaskCompletionSource<bool> waitCompletionSource)
        {
            this._waitCompletionSource = waitCompletionSource;
        }

        public override Task process(WatchedEvent @event)
        {
            // only care about connected state events; the ConnectionLostToken takes care of the other states for us
            if (@event.getState() == Event.KeeperState.SyncConnected)
            {
                this._waitCompletionSource.TrySetResult(true);
            }

            return Task.CompletedTask;
        }
    }

    public record State(string EphemeralNodePath, (string Path, int SequenceNumber, string Prefix)[] SortedChildren);
}
