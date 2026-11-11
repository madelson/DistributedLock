using Azure;
using Azure.Data.Tables;
using Medallion.Threading.Internal;
using System.Text;

namespace Medallion.Threading.Azure;

/// <summary>
/// Implements a <see cref="IDistributedLock"/> based on Azure table storage entities with
/// optimistic concurrency (ETags) simulating a lease.
/// </summary>
public sealed partial class AzureTableDistributedLock : IInternalDistributedLock<AzureTableDistributedLockHandle>
{
    internal const string DefaultPartitionKey = "__DistributedLock";

    private readonly TableClientWrapper _tableClient;
    private readonly AzureTableOptions _options;

    /// <summary>
    /// Implements <see cref="IDistributedLock.Name"/>. This is the application-level name of the lock;
    /// see <see cref="RowKey"/> for the storage coordinate actually used.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The partition key of the entity backing this lock.
    /// </summary>
    public string PartitionKey { get; }

    /// <summary>
    /// The row key of the entity backing this lock. By default this is <see cref="Name"/> sanitized to
    /// remove characters disallowed in key fields, but it may be customized via
    /// <see cref="AzureTableOptionsBuilder.RowKey(AzureTableKeySelector)"/>.
    /// </summary>
    public string RowKey { get; }

    /// <summary>
    /// Constructs a lock using a row named <paramref name="name"/> within <paramref name="tableClient"/>.
    /// </summary>
    public AzureTableDistributedLock(TableClient tableClient, string name, Action<AzureTableOptionsBuilder>? options = null)
    {
        this._tableClient = new(tableClient ?? throw new ArgumentNullException(nameof(tableClient)));
        if (name == null) { throw new ArgumentNullException(nameof(name)); }

        this._options = AzureTableOptionsBuilder.GetOptions(options);
        this.Name = name;

        // Selectors are applied exactly once, here. Acquire, renew, and release must all compute the
        // same keys, so caching turns that correctness requirement into a structural guarantee.
        this.PartitionKey = this._options.PartitionKey is { } partitionKeySelector
            ? partitionKeySelector.SelectAndValidate(name, "PartitionKey")
            : DefaultPartitionKey;
        this.RowKey = this._options.RowKey is { } rowKeySelector
            ? rowKeySelector.SelectAndValidate(name, "RowKey")
            : GetSafeName(name);
    }

    // Row keys may not contain '/', '\', '#', '?' or control chars. While the documented size limit is
    // 1KiB, the service rejects keys well below that, so we cap conservatively at 512 chars.
    // https://learn.microsoft.com/en-us/rest/api/storageservices/understanding-the-table-service-data-model
    internal static string GetSafeName(string name) =>
        // The outer conversion is required because the base64 hash ToSafeName appends may itself
        // contain '/', which is not legal in a key field.
        ConvertToValidName(DistributedLockHelpers.ToSafeName(name, maxNameLength: 512, ConvertToValidName));

    private static string ConvertToValidName(string name)
    {
        if (name.Length == 0) { return "__EMPTY__"; }

        StringBuilder? builder = null;
        for (var i = 0; i < name.Length; ++i)
        {
            var @char = name[i];
            if (@char is '/' or '\\' or '#' or '?' || char.IsControl(@char))
            {
                (builder ??= new StringBuilder().Append(name, 0, i)).Append('_');
            }
            else
            {
                builder?.Append(@char);
            }
        }

        return builder?.ToString() ?? name;
    }

    ValueTask<AzureTableDistributedLockHandle?> IInternalDistributedLock<AzureTableDistributedLockHandle>.InternalTryAcquireAsync(
        TimeoutValue timeout,
        CancellationToken cancellationToken) =>
        BusyWaitHelper.WaitAsync(
            (@lock: this, state: new AcquireState()),
            (s, token) => s.@lock.TryAcquireAsync(s.state, token, isRetryAfterCreate: false),
            timeout,
            minSleepTime: this._options.MinBusyWaitSleepTime,
            maxSleepTime: this._options.MaxBusyWaitSleepTime,
            cancellationToken
        );

    private async ValueTask<AzureTableDistributedLockHandle?> TryAcquireAsync(
        AcquireState state,
        CancellationToken cancellationToken,
        bool isRetryAfterCreate)
    {
        try
        {
            return await this.TryAcquireInternalAsync(state, cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException ex)
            when (ex.Status == 404
                && ex.ErrorCode == AzureErrors.TableNotFound
                && this._options.CreateTableIfNotExists
                && !isRetryAfterCreate)
        {
            // Remediate rather than pre-create: this costs nothing in the common case where the table
            // already exists. CreateIfNotExists is idempotent server-side, so racing callers are fine.
            await this._tableClient.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
            return await this.TryAcquireAsync(state, cancellationToken, isRetryAfterCreate: true).ConfigureAwait(false);
        }
    }

    private async ValueTask<AzureTableDistributedLockHandle?> TryAcquireInternalAsync(
        AcquireState state,
        CancellationToken cancellationToken)
    {
        var context = new AzureTableAcquireContext(
            this.Name, this.PartitionKey, this.RowKey, state.AttemptNumber++, state.RowExists);
        var strategy = this._options.AcquireStrategy.Select(context);

        var lockId = state.LockId;
        var entity = this.CreateEntity(lockId);

        if (strategy == AzureTableAcquireStrategy.InsertFirst
            && await TryInsertAsync().ConfigureAwait(false) is { } insertedHandle)
        {
            return insertedHandle;
        }

        var existing = await this._tableClient
            .GetEntityIfExistsAsync<TableLockEntity>(this.PartitionKey, this.RowKey, cancellationToken)
            .ConfigureAwait(false);
        state.RowExists = existing.HasValue;

        if (!existing.HasValue)
        {
            // Released between probes; take it now rather than burning a full sleep interval. This
            // may be a second insert attempt if we already raced and lost above, but it is bounded:
            // the read proved the row is gone, and this is the only other call site.
            return await TryInsertAsync().ConfigureAwait(false);
        }

        var current = existing.Value!;

        // An earlier request of ours may have landed even though we saw it fail.
        // Only adopt it if it hasn't expired; an expired row falls through to the steal path.
        if (current.LockId == lockId
            && (current.ExpiresAt is not { } ownExpiry || ownExpiry > DateTimeOffset.UtcNow))
        {
            return this.CreateHandle(lockId, current.ETag);
        }

        // A null ExpiresAt means an infinite-duration lease, which never expires.
        if (existing.Value!.ExpiresAt is not { } expiresAt || expiresAt > DateTimeOffset.UtcNow) { return null; }

        // Steal the expired lease, but only if nobody else mutated the row since we read it.
        try
        {
            var eTag = await this._tableClient
                .UpdateEntityAsync(entity, existing.Value.ETag, cancellationToken)
                .ConfigureAwait(false);
            return this.CreateHandle(lockId, eTag);
        }
        catch (RequestFailedException ex) when (ex.Status is 412 or 404)
        {
            return null; // lost the race to steal
        }

        async ValueTask<AzureTableDistributedLockHandle?> TryInsertAsync()
        {
            try
            {
                var eTag = await this._tableClient.AddEntityAsync(entity, cancellationToken).ConfigureAwait(false);
                state.RowExists = false; // it did not exist until we created it
                return this.CreateHandle(lockId, eTag);
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                state.RowExists = true;
                return null;
            }
        }
    }

    internal TableLockEntity CreateEntity(string lockId) =>
        new()
        {
            PartitionKey = this.PartitionKey,
            RowKey = this.RowKey,
            LockId = lockId,
            // An infinite duration is represented as a null expiry, i.e. "never expires".
            ExpiresAt = this._options.Duration.IsInfinite
                ? null
                : DateTimeOffset.UtcNow + this._options.Duration.TimeSpan,
        };

    private AzureTableDistributedLockHandle CreateHandle(string lockId, ETag eTag) =>
        new(new InternalHandle(this, lockId, eTag));

    /// <summary>
    /// Mutable probe state for a single acquire call (i.e. one <see cref="BusyWaitHelper"/> loop).
    /// </summary>
    private sealed class AcquireState
    {
        public int AttemptNumber;
        public bool? RowExists;
        public readonly string LockId = Guid.NewGuid().ToString("N");
    }

    internal sealed class TableLockEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public string LockId { get; set; } = null!;
        public DateTimeOffset? ExpiresAt { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}