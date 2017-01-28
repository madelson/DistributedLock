using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql.ConnectionPooling
{
    internal sealed class SharedConnectionLock
    {
        // always use 0, since this is meant to be optimistic. We don't want to use a longer
        // wait that would block use of the shared connection
        private const int AcquireCommandTimeoutMillis = 0;

        private static readonly IReadOnlyList<string> EmptyArray = Enumerable.Empty<string>() as string[] ?? new string[0];

        private readonly Dictionary<string, WeakReference<IDisposable>> heldLocks =
            new Dictionary<string, WeakReference<IDisposable>>(StringComparer.OrdinalIgnoreCase);
        private readonly SemaphoreSlim @lock = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        private int writesUntilNextPurge = 1;
        private readonly SqlConnection connection;

        public SharedConnectionLock(string connectionString)
        {
            this.connection = new SqlConnection(connectionString);
        }
        
        public PooledConnectionLockResult? TryAcquire(string lockName, SqlApplicationLock.Mode mode)
        {
            if (@lock.Wait(TimeSpan.Zero))
            {
                try
                {
                    // if this connection is already holding the lock on behalf of someone else, communicate that
                    WeakReference<IDisposable> heldLockReference;
                    if (this.heldLocks.TryGetValue(lockName, out heldLockReference))
                    {
                        IDisposable heldLockHandle;
                        if (heldLockReference.TryGetTarget(out heldLockHandle))
                        {
                            return new PooledConnectionLockResult();
                        }
                    }

                    if (this.connection.State != ConnectionState.Open) { this.connection.Open(); }

                    if (!SqlApplicationLock.ExecuteAcquireCommand(this.connection, lockName, AcquireCommandTimeoutMillis, mode))
                    {
                        return new PooledConnectionLockResult();
                    }

                    var purgedLockNames = this.PurgeIfNeededNoLock();
                    var successResult = this.RegisterAndCreateSuccessResultNoLock(lockName);

                    for (var i = 0; i < purgedLockNames.Count; ++i)
                    {
                        IDbDataParameter ignored; // note: we don't check the exit code; this is just best-effort cleanup
                        using (var releaseCommand = SqlApplicationLock.CreateReleaseCommand(this.connection, purgedLockNames[i], out ignored))
                        {
                            releaseCommand.ExecuteNonQuery();
                        }
                    }

                    return successResult;
                }
                finally
                {
                    @lock.Release();
                }
            }
            else
            {
                // if the connection is busy, just return immediately indicating that
                return null;
            }
        }

        public async Task<PooledConnectionLockResult?> TryAcquireAsync(string lockName, SqlApplicationLock.Mode mode)
        {
            if (await @lock.WaitAsync(TimeSpan.Zero).ConfigureAwait(false))
            {
                try
                {
                    // if this connection is already holding the lock on behalf of someone else, communicate that
                    if (this.heldLocks.ContainsKey(lockName)) { return new PooledConnectionLockResult(); }

                    if (this.connection.State != ConnectionState.Open)
                    {
                        await this.connection.OpenAsync().ConfigureAwait(false);
                    }

                    if (!await SqlApplicationLock.ExecuteAcquireCommandAsync(this.connection, lockName, AcquireCommandTimeoutMillis, mode, CancellationToken.None))
                    {
                        return new PooledConnectionLockResult();
                    }

                    var purgedLockNames = this.PurgeIfNeededNoLock();
                    var successResult = this.RegisterAndCreateSuccessResultNoLock(lockName);

                    for (var i = 0; i < purgedLockNames.Count; ++i)
                    {
                        IDbDataParameter ignored; // note: we don't check the exit code; this is just best-effort cleanup
                        using (var releaseCommand = SqlApplicationLock.CreateReleaseCommand(this.connection, purgedLockNames[i], out ignored))
                        {
                            await releaseCommand.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
                        }
                    }

                    return successResult;
                }
                finally
                {
                    @lock.Release();
                }
            }
            else
            {
                // if the connection is busy, just return immediately indicating that
                return null;
            }
        }

        private void OnLockScopeDisposed(string lockName)
        {
            @lock.Wait();
            try
            {
                this.heldLocks.Remove(lockName);
                SqlApplicationLock.ExecuteReleaseCommand(this.connection, lockName);

                // don't hold the connection open if we don't need to
                if (this.heldLocks.Count == 0)
                {
                    this.connection.Close();
                }
            }
            finally
            {
                @lock.Release();
            }
        }

        private IReadOnlyList<string> PurgeIfNeededNoLock()
        {
            if (this.writesUntilNextPurge > 0) { return EmptyArray; }

            var lockNamesToRelease = new List<string>();
            foreach (var kvp in this.heldLocks)
            {
                IDisposable ignored;
                if (!kvp.Value.TryGetTarget(out ignored))
                {
                    (lockNamesToRelease ?? (lockNamesToRelease = new List<string>())).Add(kvp.Key);
                }
            }

            lockNamesToRelease?.ForEach(n => this.heldLocks.Remove(n));
            this.writesUntilNextPurge = Math.Max(this.heldLocks.Count, 1);
            return lockNamesToRelease ?? EmptyArray;
        }

        private PooledConnectionLockResult RegisterAndCreateSuccessResultNoLock(string lockName)
        {
            var scope = new LockScope(this, lockName);
            this.heldLocks.Add(lockName, new WeakReference<IDisposable>(scope));
            --this.writesUntilNextPurge;
            return new PooledConnectionLockResult(scope);
        }

        private sealed class LockScope : IDisposable
        {
            private SharedConnectionLock @lock;
            private readonly string lockName;

            public LockScope(SharedConnectionLock @lock, string lockName)
            {
                this.@lock = @lock;
                this.lockName = lockName;
            }
            
            public void Dispose()
            {
                var @lock = Interlocked.Exchange(ref this.@lock, null);
                @lock?.OnLockScopeDisposed(this.lockName);
            }
        }
    }

    internal struct PooledConnectionLockResult
    {
        public PooledConnectionLockResult(IDisposable handle)
        {
            this.Handle = handle;
        }

        public IDisposable Handle { get; }
    }
}
