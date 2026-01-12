using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using System.Data;

namespace Medallion.Threading.GBase;

/// <summary>
/// Implements a distributed lock for GBase databse based on the DBMS_LOCK package
/// </summary>
public sealed partial class GBaseDistributedLock : IInternalDistributedLock<GBaseDistributedLockHandle>
{
    internal const int MaxNameLength = 128;

    private readonly IDbDistributedLock _internalLock;

    /// <summary>
    /// Constructs a lock with the given <paramref name="name"/> that connects using the provided <paramref name="connectionString"/> and
    /// <paramref name="options"/>.
    /// 
    /// Unless <paramref name="exactName"/> is specified, <paramref name="name"/> will be escaped/hashed to ensure name validity.
    /// </summary>
    public GBaseDistributedLock(string name, string connectionString, Action<GBaseConnectionOptionsBuilder>? options = null, bool exactName = false)
        : this(name, exactName, n => CreateInternalLock(n, connectionString, options))
    {
    }

    /// <summary>
    /// Constructs a lock with the given <paramref name="name"/> that connects using the provided <paramref name="connection" />.
    /// 
    /// Unless <paramref name="exactName"/> is specified, <paramref name="name"/> will be escaped/hashed to ensure name validity.
    /// </summary>
    public GBaseDistributedLock(string name, IDbConnection connection, bool exactName = false)
        : this(name, exactName, n => CreateInternalLock(n, connection))
    {
    }

    private GBaseDistributedLock(string name, bool exactName, Func<string, IDbDistributedLock> internalLockFactory)
    {
        this.Name = GetName(name, exactName);
        this._internalLock = internalLockFactory(this.Name);
    }

    internal static string GetName(string name, bool exactName)
    {
        if (name == null) { throw new ArgumentNullException(nameof(name)); }

        if (exactName)
        {
            if (name.Length > MaxNameLength) { throw new FormatException($"{nameof(name)}: must be at most {MaxNameLength} characters"); }
            if (name.Length == 0) { throw new FormatException($"{nameof(name)} must not be empty"); }
            return name;
        }
        
        return DistributedLockHelpers.ToSafeName(name, MaxNameLength, s => s.Length == 0 ? "EMPTY" : s);
    }

    /// <summary>
    /// Implements <see cref="IDistributedLock.Name"/>
    /// </summary>
    public string Name { get; }

    ValueTask<GBaseDistributedLockHandle?> IInternalDistributedLock<GBaseDistributedLockHandle>.InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken) =>
        this._internalLock.TryAcquireAsync(timeout, GBaseDbmsLock.ExclusiveLock, cancellationToken, contextHandle: null).Wrap(h => new GBaseDistributedLockHandle(h));

    internal static IDbDistributedLock CreateInternalLock(string name, string connectionString, Action<GBaseConnectionOptionsBuilder>? options)
    {
        if (connectionString == null) { throw new ArgumentNullException(nameof(connectionString)); }

        var (keepaliveCadence, useMultiplexing) = GBaseConnectionOptionsBuilder.GetOptions(options);

        if (useMultiplexing)
        {
            return new OptimisticConnectionMultiplexingDbDistributedLock(name, connectionString, GBaseMultiplexedConnectionLockPool.Instance, keepaliveCadence);
        }

        return new DedicatedConnectionOrTransactionDbDistributedLock(name, () => new GBaseDatabaseConnection(connectionString), useTransaction: false, keepaliveCadence);
    }

    internal static IDbDistributedLock CreateInternalLock(string name, IDbConnection connection)
    {
        if (connection == null) { throw new ArgumentNullException(nameof(connection)); }

        return new DedicatedConnectionOrTransactionDbDistributedLock(name, () => new GBaseDatabaseConnection(connection));
    }
}
