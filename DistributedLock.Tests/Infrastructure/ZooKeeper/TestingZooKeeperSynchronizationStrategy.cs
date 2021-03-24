using Medallion.Threading.ZooKeeper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Medallion.Threading.Tests.ZooKeeper
{
    public sealed class TestingZooKeeperSynchronizationStrategy : TestingSynchronizationStrategy
    {
        private List<string>? _trackedPaths;

        public bool AssumeNodeExists { get; set; }

        public Action<ZooKeeperDistributedSynchronizationOptionsBuilder>? Options { get; set; }

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

                // delete the newest child of the node (other children may be extra semaphore ticket holders)
                foreach (var trackedPath in trackedPaths)
                {
                    var childrenResult = connection.ZooKeeper.getChildrenAsync(trackedPath).Result;
                    var toDelete = childrenResult.Children.Select(ch => $"{trackedPath.TrimEnd(ZooKeeperPath.Separator)}{ZooKeeperPath.Separator}{ch}")
                        .Select(p => (Path: p, CreationTime: connection.ZooKeeper.existsAsync(p).Result?.getCtime() ?? -1))
                        .OrderByDescending(t => t.CreationTime)
                        .First();
                    connection.ZooKeeper.deleteAsync(toDelete.Path).Wait();
                }
            }
        }
    }
}
