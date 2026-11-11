using Azure;
using Azure.Data.Tables;
using Medallion.Threading.Internal;

namespace Medallion.Threading.Azure;

/// <summary>
/// Adds <see cref="SyncViaAsync"/> support to <see cref="TableClient"/>
/// </summary>
internal class TableClientWrapper
{
    private readonly TableClient _tableClient;

    public TableClientWrapper(TableClient tableClient)
    {
        this._tableClient = tableClient;
    }

    public ValueTask CreateIfNotExistsAsync(CancellationToken cancellationToken)
    {
        if (SyncViaAsync.IsSynchronous)
        {
            this._tableClient.CreateIfNotExists(cancellationToken);
            return default;
        }
        return new(this._tableClient.CreateIfNotExistsAsync(cancellationToken));
    }

    public async ValueTask<NullableResponse<T>> GetEntityIfExistsAsync<T>(string partitionKey, string rowKey, CancellationToken cancellationToken)
        where T : class, ITableEntity =>
        SyncViaAsync.IsSynchronous
            ? this._tableClient.GetEntityIfExists<T>(partitionKey, rowKey, cancellationToken: cancellationToken)
            : await this._tableClient.GetEntityIfExistsAsync<T>(partitionKey, rowKey, cancellationToken: cancellationToken).ConfigureAwait(false);

    public async ValueTask<ETag> AddEntityAsync<T>(T entity, CancellationToken cancellationToken)
        where T : ITableEntity
    {
        var response = SyncViaAsync.IsSynchronous
            ? this._tableClient.AddEntity(entity, cancellationToken)
            : await this._tableClient.AddEntityAsync(entity, cancellationToken).ConfigureAwait(false);
        return response.Headers.ETag!.Value;
    }

    public async ValueTask<ETag> UpdateEntityAsync<T>(T entity, ETag ifMatch, CancellationToken cancellationToken)
        where T : ITableEntity
    {
        var response = SyncViaAsync.IsSynchronous
            ? this._tableClient.UpdateEntity(entity, ifMatch, TableUpdateMode.Replace, cancellationToken)
            : await this._tableClient.UpdateEntityAsync(entity, ifMatch, TableUpdateMode.Replace, cancellationToken).ConfigureAwait(false);
        return response.Headers.ETag!.Value;
    }

    public ValueTask DeleteEntityAsync(string partitionKey, string rowKey, ETag ifMatch, CancellationToken cancellationToken)
    {
        if (SyncViaAsync.IsSynchronous)
        {
            this._tableClient.DeleteEntity(partitionKey, rowKey, ifMatch, cancellationToken);
            return default;
        }
        return new(this._tableClient.DeleteEntityAsync(partitionKey, rowKey, ifMatch, cancellationToken));
    }
}
