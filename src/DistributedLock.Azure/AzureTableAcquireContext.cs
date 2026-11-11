namespace Medallion.Threading.Azure;

/// <summary>
/// Context passed to an <see cref="AzureTableAcquireStrategySelector"/> before each acquire attempt.
/// </summary>
public readonly struct AzureTableAcquireContext : IEquatable<AzureTableAcquireContext>
{
    internal AzureTableAcquireContext(string name, string partitionKey, string rowKey, int attemptNumber, bool? rowExists)
    {
        this.Name = name;
        this.PartitionKey = partitionKey;
        this.RowKey = rowKey;
        this.AttemptNumber = attemptNumber;
        this.RowExists = rowExists;
    }

    /// <summary>
    /// The application-level lock name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The resolved partition key of the backing entity.
    /// </summary>
    public string PartitionKey { get; }

    /// <summary>
    /// The resolved row key of the backing entity.
    /// </summary>
    public string RowKey { get; }

    /// <summary>
    /// Zero-based index of this attempt within the current acquire (busy-wait) loop. Note that this
    /// resets for each acquire call; it does not accumulate over the lifetime of the lock object.
    /// </summary>
    public int AttemptNumber { get; }

    /// <summary>
    /// Whether the row was observed to exist during the previous attempt, or null if not yet known
    /// (i.e. when <see cref="AttemptNumber"/> is 0).
    ///
    /// This is an observation which may already be stale; it is a hint for optimization only and must
    /// never be relied upon for correctness.
    /// </summary>
    public bool? RowExists { get; }

    /// <summary>
    /// Compares two <see cref="AzureTableAcquireContext"/>s for equality.
    /// </summary>
    public bool Equals(AzureTableAcquireContext that) =>
        this.Name == that.Name
            && this.PartitionKey == that.PartitionKey
            && this.RowKey == that.RowKey
            && this.AttemptNumber == that.AttemptNumber
            && this.RowExists == that.RowExists;

    /// <summary>
    /// Compares this to another object for equality.
    /// </summary>
    public override bool Equals(object? obj) => obj is AzureTableAcquireContext that && this.Equals(that);

    /// <summary>
    /// Gets a hash code for this instance.
    /// </summary>
    public override int GetHashCode()
    {
        var hash = 17;
        hash = (hash * 31) + (this.Name?.GetHashCode() ?? 0);
        hash = (hash * 31) + (this.PartitionKey?.GetHashCode() ?? 0);
        hash = (hash * 31) + (this.RowKey?.GetHashCode() ?? 0);
        hash = (hash * 31) + this.AttemptNumber;
        hash = (hash * 31) + this.RowExists.GetHashCode();
        return hash;
    }

    /// <summary>
    /// Compares two <see cref="AzureTableAcquireContext"/>s for equality.
    /// </summary>
    public static bool operator ==(AzureTableAcquireContext a, AzureTableAcquireContext b) => a.Equals(b);

    /// <summary>
    /// Compares two <see cref="AzureTableAcquireContext"/>s for inequality.
    /// </summary>
    public static bool operator !=(AzureTableAcquireContext a, AzureTableAcquireContext b) => !a.Equals(b);
}
