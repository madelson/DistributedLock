using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading.FileSystem;
using Medallion.Threading.Postgres;
using Medallion.Threading.WaitHandles;

namespace Medallion.Threading.Tests;

public class TestingCompositeFileDistributedLock(string name) : IDistributedLock
{
    private readonly FileDistributedSynchronizationProvider _provider = new(
        new DirectoryInfo(Path.Combine(Path.GetTempPath(), typeof(TestingCompositeFileDistributedLock).Name)));
    private readonly string[] _names = [name + "_1", name + "_2"];

    public string Name => name;

    public IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        this._provider.AcquireAllLocks(this._names, timeout, cancellationToken);

    public ValueTask<IDistributedSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        this._provider.AcquireAllLocksAsync(this._names, timeout, cancellationToken);

    public IDistributedSynchronizationHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        this._provider.TryAcquireAllLocks(this._names, timeout, cancellationToken);

    public ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        this._provider.TryAcquireAllLocksAsync(this._names, timeout, cancellationToken);
}

public class TestingCompositeWaitHandleDistributedSemaphore(string name, int maxCount) : IDistributedSemaphore
{
    private readonly WaitHandleDistributedSynchronizationProvider _provider = new();
    private readonly string[] _names = [name + "_1", name + "_2"];

    public string Name => name;

    public int MaxCount => maxCount;

    public IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        this._provider.AcquireAllSemaphores(this._names, maxCount, timeout, cancellationToken);

    public ValueTask<IDistributedSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        this._provider.AcquireAllSemaphoresAsync(this._names, maxCount, timeout, cancellationToken);

    public IDistributedSynchronizationHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        this._provider.TryAcquireAllSemaphores(this._names, maxCount, timeout, cancellationToken);

    public ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        this._provider.TryAcquireAllSemaphoresAsync(this._names, maxCount, timeout, cancellationToken);
}

public class TestingCompositePostgresReaderWriterLock(string name, string connectionString, Action<PostgresConnectionOptionsBuilder>? options = null) : IDistributedReaderWriterLock
{
    private readonly PostgresDistributedSynchronizationProvider _provider = new(connectionString, options);
    private readonly string[] _names = [name + "_1", name + "_2"];

    public string Name => name;

    public IDistributedSynchronizationHandle AcquireReadLock(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        this._provider.AcquireAllReadLocks(this._names, timeout, cancellationToken);

    public ValueTask<IDistributedSynchronizationHandle> AcquireReadLockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        this._provider.AcquireAllReadLocksAsync(this._names, timeout, cancellationToken);

    public IDistributedSynchronizationHandle AcquireWriteLock(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        this._provider.AcquireAllWriteLocks(this._names, timeout, cancellationToken);

    public ValueTask<IDistributedSynchronizationHandle> AcquireWriteLockAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        this._provider.AcquireAllWriteLocksAsync(this._names, timeout, cancellationToken);

    public IDistributedSynchronizationHandle? TryAcquireReadLock(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        this._provider.TryAcquireAllReadLocks(this._names, timeout, cancellationToken);

    public ValueTask<IDistributedSynchronizationHandle?> TryAcquireReadLockAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        this._provider.TryAcquireAllReadLocksAsync(this._names, timeout, cancellationToken);

    public IDistributedSynchronizationHandle? TryAcquireWriteLock(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        this._provider.TryAcquireAllWriteLocks(this._names, timeout, cancellationToken);

    public ValueTask<IDistributedSynchronizationHandle?> TryAcquireWriteLockAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        this._provider.TryAcquireAllWriteLocksAsync(this._names, timeout, cancellationToken);
}