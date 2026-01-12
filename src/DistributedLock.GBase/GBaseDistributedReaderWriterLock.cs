using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using System.Data;

namespace Medallion.Threading.GBase;

/// <summary>
/// Implements an upgradeable distributed reader-writer lock for the GBase database using the DBMS_LOCK package.
/// </summary>
public sealed partial class GBaseDistributedReaderWriterLock : IInternalDistributedUpgradeableReaderWriterLock<GBaseDistributedReaderWriterLockHandle, GBaseDistributedReaderWriterLockUpgradeableHandle>
{
    private readonly IDbDistributedLock _internalLock;

    /// <summary>
    /// Constructs a new lock using the provided <paramref name="name"/>. 
    /// 
    /// The provided <paramref name="connectionString"/> will be used to connect to the database.
    /// 
    /// Unless <paramref name="exactName"/> is specified, <paramref name="name"/> will be escaped/hashed to ensure name validity.
    /// </summary>
    public GBaseDistributedReaderWriterLock(string name, string connectionString, Action<GBaseConnectionOptionsBuilder>? options = null, bool exactName = false)
        : this(name, exactName, n => GBaseDistributedLock.CreateInternalLock(n, connectionString, options))
    {
    }

    /// <summary>
    /// Constructs a new lock using the provided <paramref name="name"/>.
    /// 
    /// The provided <paramref name="connection"/> will be used to connect to the database and will provide lock scope. It is assumed to be externally managed and
    /// will not be opened or closed.
    /// 
    /// Unless <paramref name="exactName"/> is specified, <paramref name="name"/> will be escaped/hashed to ensure name validity.
    /// </summary>
    public GBaseDistributedReaderWriterLock(string name, IDbConnection connection, bool exactName = false)
        : this(name, exactName, n => GBaseDistributedLock.CreateInternalLock(n, connection))
    {
    }

    private GBaseDistributedReaderWriterLock(string name, bool exactName, Func<string, IDbDistributedLock> internalLockFactory)
    {
        this.Name = GBaseDistributedLock.GetName(name, exactName);
        this._internalLock = internalLockFactory(this.Name);
    }

    /// <summary>
    /// Implements <see cref="IDistributedLock.Name"/>
    /// </summary>
    public string Name { get; }

    async ValueTask<GBaseDistributedReaderWriterLockUpgradeableHandle?> IInternalDistributedUpgradeableReaderWriterLock<GBaseDistributedReaderWriterLockHandle, GBaseDistributedReaderWriterLockUpgradeableHandle>.InternalTryAcquireUpgradeableReadLockAsync(
        TimeoutValue timeout,
        CancellationToken cancellationToken)
    {
        var innerHandle = await this._internalLock
            .TryAcquireAsync(timeout, GBaseDbmsLock.UpdateLock, cancellationToken, contextHandle: null).ConfigureAwait(false);
        return innerHandle != null ? new GBaseDistributedReaderWriterLockUpgradeableHandle(innerHandle, this._internalLock) : null;
    }

    async ValueTask<GBaseDistributedReaderWriterLockHandle?> IInternalDistributedReaderWriterLock<GBaseDistributedReaderWriterLockHandle>.InternalTryAcquireAsync(
        TimeoutValue timeout,
        CancellationToken cancellationToken,
        bool isWrite)
    {
        var innerHandle = await this._internalLock
            .TryAcquireAsync(timeout, isWrite ? GBaseDbmsLock.ExclusiveLock : GBaseDbmsLock.SharedLock, cancellationToken, contextHandle: null).ConfigureAwait(false);
        return innerHandle != null ? new GBaseDistributedReaderWriterLockNonUpgradeableHandle(innerHandle) : null;
    }
}
