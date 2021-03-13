using Medallion.Threading.Internal;
using org.apache.zookeeper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.ZooKeeper
{
    /// <summary>
    /// <see cref="IDistributedSynchronizationHandle"/> implementation where holding the primitive
    /// is based on the existence of an ephemeral zookeeper node
    /// </summary>
    internal sealed class ZooKeeperNodeHandle : IDistributedSynchronizationHandle
    {
        private readonly ZooKeeperConnection _connection;
        private readonly ZooKeeperPath _nodePath;
        private readonly bool _shouldDeleteParent;
        private readonly Lazy<HandleLostState> _handleLostState;

        private volatile bool _disposed;

        public ZooKeeperNodeHandle(ZooKeeperConnection connection, string nodePath, bool shouldDeleteParent)
        {
            this._connection = connection;
            this._nodePath = new ZooKeeperPath(nodePath);
            this._shouldDeleteParent = shouldDeleteParent;

            this._handleLostState = new Lazy<HandleLostState>(() =>
            {
                var handleLostSource = CancellationTokenSource.CreateLinkedTokenSource(this._connection.ConnectionLostToken);
                var handleLostToken = handleLostSource.Token; // grab this now before the source is disposed
                var disposalSource = new CancellationTokenSource();
                var disposalSourceToken = disposalSource.Token;
                var monitoringTask = Task.Run(async () =>
                {
                    try
                    {
                        while (true)
                        {
                            var result = await this._connection.WaitForNotExistsOrChanged(
                                this._nodePath.ToString(), 
                                cancellationToken: CancellationToken.None, // ConnectionLostToken already accounted for by method 
                                timeoutToken: disposalSource.Token
                            ).ConfigureAwait(false);
                            switch (result)
                            {
                                case false: // disposalSource triggered
                                    return;
                                case true: // node no longer exists
                                    handleLostSource.Cancel();
                                    return;
                                default: // something changed
                                    break; // continue looping
                            }
                        }
                    }
                    finally
                    {
                        handleLostSource.Dispose();
                    }
                });
                return new HandleLostState(handleLostToken, disposalSource, monitoringTask);
            });
        }

        public CancellationToken HandleLostToken => this._disposed ? throw this.ObjectDisposed() : this._handleLostState.Value.Token;

        public void Dispose() => this.DisposeSyncViaAsync();

        public ValueTask DisposeAsync()
        {
            var disposalTask = this.InternalDisposeAsync();
            if (!SyncViaAsync.IsSynchronous) { return disposalTask.AsValueTask(); }
            // we're forced to use sync-over-async here because ZooKeeperNetEx doesn't have synchronous APIs
            disposalTask.GetAwaiter().GetResult();
            return default;
        }

        private async Task InternalDisposeAsync()
        {
            if (this._disposed) { return; }
            this._disposed = true;

            try
            {
                // clean up monitoring
                if (this._handleLostState.IsValueCreated)
                {
                    this._handleLostState.Value.DisposalSource.Cancel();
                    this._handleLostState.Value.DisposalSource.Dispose();
                    await this._handleLostState.Value.MonitoringTask.ConfigureAwait(false);
                }
            }
            finally
            {
                try
                {
                    // clean up the node
                    await this._connection.ZooKeeper.deleteAsync(this._nodePath.ToString()).ConfigureAwait(false);

                    if (this._shouldDeleteParent)
                    {
                        try { await this._connection.ZooKeeper.deleteAsync(this._nodePath.GetDirectory()!.Value.ToString()).ConfigureAwait(false); }
                        catch (KeeperException.NotEmptyException) { } // can't delete nodes which have other children
                        catch (KeeperException.NoNodeException) { } // can't delete nodes which don't exist (race condition)
                    }
                }
                finally
                {
                    this._connection.Dispose();
                }
            }
        }

        private record HandleLostState(CancellationToken Token, CancellationTokenSource DisposalSource, Task MonitoringTask);
    }
}
