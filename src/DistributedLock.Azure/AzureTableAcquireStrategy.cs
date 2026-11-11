namespace Medallion.Threading.Azure;

/// <summary>
/// How a single acquire attempt probes the entity backing an <see cref="AzureTableDistributedLock"/>.
/// </summary>
public enum AzureTableAcquireStrategy
{
    /// <summary>
    /// Attempt an insert first, falling back to a read if the row already exists. Completes an
    /// uncontended acquire in a single request.
    /// </summary>
    InsertFirst = 0,

    /// <summary>
    /// Read before attempting an insert. Avoids a wasted request (and a thrown
    /// <see cref="global::Azure.RequestFailedException"/>) when the row is expected to already exist.
    /// </summary>
    ReadFirst = 1,
}
