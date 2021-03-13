using Medallion.Threading.ZooKeeper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Medallion.Threading.Tests.ZooKeeper
{
    public sealed class TestingZooKeeperSynchronizationStrategy : TestingSynchronizationStrategy
    {
        private List<string>? _trackedPaths;

        public void TrackPath(string path) => this._trackedPaths?.Add(path);

        public override IDisposable? PrepareForHandleLost()
        {
            if (this._trackedPaths != null) { throw new InvalidOperationException("Already in handle lost mode"); }

            this._trackedPaths = new List<string>();
            return new HandleLostScope(this);
        }

        private class HandleLostScope : IDisposable
        {
            private readonly TestingZooKeeperSynchronizationStrategy _strategy;

            public HandleLostScope(TestingZooKeeperSynchronizationStrategy strategy)
            {
                this._strategy = strategy;
            }

            public void Dispose()
            {
                var trackedPaths = Interlocked.Exchange(ref this._strategy._trackedPaths, null);
                if (trackedPaths == null) { return; } // already disposed

                using var connection = ZooKeeperConnection.DefaultPool.ConnectAsync(
                        new ZooKeeperConnectionInfo(ZooKeeperPorts.DefaultConnectionString, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30), new EquatableReadOnlyList<ZooKeeperAuthInfo>(Array.Empty<ZooKeeperAuthInfo>())),
                        CancellationToken.None
                    )
                    .Result;

                // delete all children of the nodes
                foreach (var trackedPath in trackedPaths)
                {
                    var childrenResult = connection.ZooKeeper.getChildrenAsync(trackedPath).Result;
                    foreach (var child in childrenResult.Children)
                    {
                        connection.ZooKeeper.deleteAsync(trackedPath + ZooKeeperPath.Separator + child).Wait();
                    }
                }
            }
        }
    }
}
