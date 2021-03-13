using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.ZooKeeper
{
    using org.apache.zookeeper;
    using org.apache.zookeeper.data;
    using System.Linq;
    using System.Threading;

    internal static class ZooKeeperHelper
    {
        /// <summary>
        /// See https://zookeeper.apache.org/doc/r3.5.4-beta/zookeeperProgrammers.html under "Builtin ACL Schemes"
        /// </summary>
        public static readonly ACL PublicAcl = new ACL(0x1f, new Id("world", "anyone"));

        /// <summary>
        /// Returns true when <paramref name="path"/> does not exist. 
        /// Returns null when we receive a watch event indicating that <paramref name="path"/> has changed. 
        /// Returns false if the <paramref name="timeoutToken"/> fires.
        /// </summary>
        public static async Task<bool?> WaitForNotExistsOrChanged(
            this ZooKeeperConnection connection,
            string path,
            CancellationToken cancellationToken,
            CancellationToken timeoutToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            using var watcher = new TaskWatcher<bool?>((_, s) => s.TrySetResult(null));
            using var cancellationTokenRegistration = cancellationToken.Register(
                state => ((TaskWatcher<bool?>)state).TaskCompletionSource.TrySetCanceled(),
                state: watcher
            );
            using var timeoutRegistration = timeoutToken.Register(
                state => ((TaskWatcher<bool?>)state).TaskCompletionSource.TrySetResult(false),
                state: watcher
            );
            // this is needed because if the connection goes down and never recovers, we'll never get the session expired notification
            using var connectionLostRegistration = connection.ConnectionLostToken.Register(
                state => ((TaskWatcher<bool?>)state).TaskCompletionSource.TrySetException(new InvalidOperationException("Lost connection to ZooKeeper")),
                state: watcher
            );

            var exists = await connection.ZooKeeper.existsAsync(path, watcher).ConfigureAwait(false);
            return exists == null ? true : await watcher.TaskCompletionSource.Task.ConfigureAwait(false);
        }

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

        private sealed class TaskWatcher<TResult> : Watcher, IDisposable
        {
            private volatile Action<WatchedEvent, TaskCompletionSource<TResult>>? _watchedEventHandler;

            public TaskWatcher(Action<WatchedEvent, TaskCompletionSource<TResult>> watchedEventHandler)
            {
                this._watchedEventHandler = watchedEventHandler;
            }

            public TaskCompletionSource<TResult> TaskCompletionSource { get; } = new TaskCompletionSource<TResult>();

            public void Dispose() => this._watchedEventHandler = null;

            public override Task process(WatchedEvent @event)
            {
                // only care about connected state events; the ConnectionLostToken takes care of the other states for us
                if (@event.getState() == Event.KeeperState.SyncConnected)
                {
                    this._watchedEventHandler?.Invoke(@event, this.TaskCompletionSource);
                }

                return Task.CompletedTask;
            }
        }
    }
}
