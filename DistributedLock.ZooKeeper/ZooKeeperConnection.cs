using Medallion.Threading.Internal;
using System.Collections;

namespace Medallion.Threading.ZooKeeper;

using org.apache.zookeeper;

/// <summary>
/// We don't want to use one session (<see cref="org.apache.zookeeper.ZooKeeper"/>) per lock because "The creation and closing of sessions are 
/// costly in ZooKeeper because they need quorum confirmations, they become the bottleneck of a ZooKeeper ensemble when it needs to handle 
/// thousands of client connections" (https://zookeeper.apache.org/doc/r3.6.2/zookeeperProgrammers.html). 
/// 
/// However, ZooKeeper sessions do "leak" watches over time (see https://issues.apache.org/jira/browse/ZOOKEEPER-442).
/// 
/// This class attempts to balance those two concerns by managing a pool of cached sessions that live for a while but not forever.
/// </summary>
internal class ZooKeeperConnection : IDisposable
{
    /// <summary>
    /// Hopefully, 10m prevents leaks from ever getting too bad while granting efficiencies by allowing us to re-use
    /// sessions under load.
    /// </summary>
    public static readonly Pool DefaultPool = new(maxAge: TimeSpan.FromMinutes(10));

    private readonly InternalConnection _internalConnection;
    private Action? _releaseToPool;

    private ZooKeeperConnection(InternalConnection internalConnection, Action releaseToPool)
    {
        this._internalConnection = internalConnection;
        this._releaseToPool = releaseToPool;
    }

    public ZooKeeper ZooKeeper => this._internalConnection.ZooKeeper;

    public CancellationToken ConnectionLostToken => this._internalConnection.ConnectionLostToken;

    public void Dispose() => Interlocked.Exchange(ref this._releaseToPool, null)?.Invoke();

    public sealed class Pool
    {
        private readonly Dictionary<ZooKeeperConnectionInfo, ConnectionEntry> Connections = new();
        private readonly TimeoutValue _maxAge;

        public Pool(TimeoutValue maxAge) 
        {
            Invariant.Require(!maxAge.IsInfinite);
            this._maxAge = maxAge.TimeSpan; 
        }

        private object PoolLock => Connections.As<ICollection>().SyncRoot;

        public Task<ZooKeeperConnection> ConnectAsync(ZooKeeperConnectionInfo connectionInfo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (this.PoolLock)
            {
                // if we have an entry in the pool, use that
                if (this.Connections.TryGetValue(connectionInfo, out var entry))
                {
                    ++entry.UserCount;
                    return ToResultAsync(entry);
                }

                // create a new connection
                var newConnectionTask = this.InternalConnectAsync(connectionInfo);
                var newEntry = new ConnectionEntry(newConnectionTask)
                {
                    // 2 because we have both the current request and the timeout task we're about to create
                    UserCount = 2
                };
                this.Connections.Add(connectionInfo, newEntry);
                newConnectionTask.ContinueWith(OnConnectionTaskCompleted);
                return ToResultAsync(newEntry);

                void OnConnectionTaskCompleted(Task<InternalConnection> internalConnectionTask)
                {
                    // if we never connected, just release our hold on the task
                    if (internalConnectionTask.Status != TaskStatus.RanToCompletion)
                    {
                        this.ReleaseEntry(connectionInfo, newEntry, remove: true);
                    }
                    // Otherwise, wait until either max age has elapsed or the connection is lost to release our hold. This
                    // ensures both that the connection won't be removed from the pool prematurely as well as that it will be
                    // eventually removed even if it stops being used
                    else
                    {
                        Task.Delay(this._maxAge.TimeSpan, internalConnectionTask.Result.ConnectionLostToken)
                            .ContinueWith(_ => this.ReleaseEntry(connectionInfo, newEntry, remove: true));
                    }
                }
            }

            async Task<ZooKeeperConnection> ToResultAsync(ConnectionEntry entry)
            {
                try
                {
                    var internalConnection = await entry.ConnectionTask.ConfigureAwait(false);
                    return new ZooKeeperConnection(internalConnection, releaseToPool: () => this.ReleaseEntry(connectionInfo, entry, remove: false));
                }
                catch
                {
                    // if we fail to construct a connection, still release our hold on the entry to allow cleanup
                    this.ReleaseEntry(connectionInfo, entry, remove: false);
                    throw;
                }
            }
        }

        private async Task<InternalConnection> InternalConnectAsync(ZooKeeperConnectionInfo connectionInfo)
        {
            var watcher = new ConnectionWatcher(connectionInfo.SessionTimeout);
            var zooKeeper = new ZooKeeper(
                connectstring: connectionInfo.ConnectionString,
                sessionTimeout: connectionInfo.SessionTimeout.InMilliseconds,
                watcher: watcher
            );

            using var timeoutSource = new CancellationTokenSource(connectionInfo.ConnectTimeout.TimeSpan);
            using var timeoutRegistration = timeoutSource.Token.Register(
                () => watcher.TaskCompletionSource.TrySetException(new TimeoutException($"Timed out connecting to ZooKeeper after {connectionInfo.ConnectTimeout.InMilliseconds}ms"))
            );

            foreach (var authInfo in connectionInfo.AuthInfo)
            {
                zooKeeper.addAuthInfo(authInfo.Scheme, authInfo.Auth.ToArray());
            }

            try
            {
                await watcher.TaskCompletionSource.Task.ConfigureAwait(false);
                return new InternalConnection(zooKeeper, watcher);
            }
            catch
            {
                // on failure, clean up the instance we created
                try { await zooKeeper.closeAsync().ConfigureAwait(false); }
                finally { watcher.Dispose(); }
                throw;
            }
        }

        private void ReleaseEntry(ZooKeeperConnectionInfo connectionInfo, ConnectionEntry entry, bool remove)
        {
            bool shouldDispose;
            lock (this.PoolLock)
            {
                if (remove)
                {
                    this.Connections.As<ICollection<KeyValuePair<ZooKeeperConnectionInfo, ConnectionEntry>>>()
                        .Remove(new KeyValuePair<ZooKeeperConnectionInfo, ConnectionEntry>(connectionInfo, entry));
                }

                shouldDispose = --entry.UserCount == 0;
                // this guarantee is upheld by the fact that we include the timeout watcher task as a "user"
                Invariant.Require(
                    !shouldDispose || !(this.Connections.TryGetValue(connectionInfo, out var registeredEntry) && registeredEntry == entry),
                    "If we're disposing then the entry must be removed from the pool"
                );
            }

            if (shouldDispose)
            {
                // kick off connection disposal in the background to
                // avoid blocking/throwing and to handle the case where the task hasn't yet completed
                entry.ConnectionTask.ContinueWith(
                    t => { var ignored = t.Result.DisposeAsync(); },
                    TaskContinuationOptions.OnlyOnRanToCompletion
                );
            }
        }

        private class ConnectionEntry
        {
            public ConnectionEntry(Task<InternalConnection> connectionTask)
            {
                this.ConnectionTask = connectionTask;
            }

            public Task<InternalConnection> ConnectionTask { get; }
            // protected by the pool's lock
            public int UserCount { get; set; }
        }
    }

    private class InternalConnection : IAsyncDisposable
    {
        private readonly ConnectionWatcher _watcher;
        
        public InternalConnection(ZooKeeper zooKeeper, ConnectionWatcher watcher)
        {
            this.ZooKeeper = zooKeeper;
            this._watcher = watcher;
        }

        public ZooKeeper ZooKeeper { get; }

        public CancellationToken ConnectionLostToken => this._watcher.ConnectionLost;

        public async ValueTask DisposeAsync()
        {
            try { await this.ZooKeeper.closeAsync().ConfigureAwait(false); }
            finally { this._watcher.Dispose(); }
        }
    }

    private class ConnectionWatcher : Watcher, IDisposable
    {
        private readonly CancellationTokenSource _connectionLostSource = new();
        private readonly TimeoutValue _sessionTimeout;
        private int _isWaitingForReconnect;

        public ConnectionWatcher(TimeoutValue sessionTimeout)
        {
            this._sessionTimeout = sessionTimeout;
        }

        public TaskCompletionSource<ValueTuple> TaskCompletionSource { get; } = new TaskCompletionSource<ValueTuple>();
        public CancellationToken ConnectionLost => this._connectionLostSource.Token;

        public override Task process(WatchedEvent @event)
        {
            if (@event.getState() == Event.KeeperState.SyncConnected)
            {
                if (!this.TaskCompletionSource.TrySetResult(default))
                {
                    // if we're reconnecting, clear any scheduled cancellation
                    this._connectionLostSource.CancelAfter(Timeout.Infinite);
                    Volatile.Write(ref this._isWaitingForReconnect, 0);
                }
            }
            else
            {
                if (!this.TaskCompletionSource.TrySetException(new InvalidOperationException($"Failed to connect to ZooKeeper. State: {@event.getState()}")))
                {
                    // if we see the expired event (which might not come if we never reconnect), just fire connection lost
                    if (@event.getState() == Event.KeeperState.Expired)
                    {
                        this._connectionLostSource.Cancel();
                    }
                    // Otherwise, zookeeper will be attempting reconnection. Give it up to the session timeout to
                    // do so before we fire. See https://zookeeper.apache.org/doc/r3.6.2/zookeeperProgrammers.html
                    else if (Interlocked.Exchange(ref this._isWaitingForReconnect, 1) == 0)
                    {
                        // Only do this if we changed from !reconnecting to reconnecting; otherwise if we get multiple
                        // failure events we'll keep re-upping the timer
                        this._connectionLostSource.CancelAfter(this._sessionTimeout.TimeSpan);
                    }
                }
            }

            return Task.CompletedTask;
        }

        public void Dispose() => this._connectionLostSource.Dispose();
    }
}
