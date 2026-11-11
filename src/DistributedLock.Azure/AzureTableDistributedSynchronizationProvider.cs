using Azure.Data.Tables;

namespace Medallion.Threading.Azure;

/// <summary>
/// Implements <see cref="IDistributedLockProvider"/> for <see cref="AzureTableDistributedLock"/>
/// </summary>
public sealed class AzureTableDistributedSynchronizationProvider : IDistributedLockProvider
{
    private readonly TableClient _tableClient;
    private readonly Action<AzureTableOptionsBuilder>? _options;

    /// <summary>
    /// Constructs a provider that scopes lock entities within the provided <paramref name="tableClient"/>
    /// and uses the provided <paramref name="options"/>.
    /// </summary>
    public AzureTableDistributedSynchronizationProvider(TableClient tableClient, Action<AzureTableOptionsBuilder>? options = null)
    {
        this._tableClient = tableClient ?? throw new ArgumentNullException(nameof(tableClient));
        this._options = options;
    }

    /// <summary>
    /// Constructs an <see cref="AzureTableDistributedLock"/> with the given <paramref name="name"/>.
    /// </summary>
    public AzureTableDistributedLock CreateLock(string name) => new(this._tableClient, name, this._options);

    IDistributedLock IDistributedLockProvider.CreateLock(string name) => this.CreateLock(name);
}
