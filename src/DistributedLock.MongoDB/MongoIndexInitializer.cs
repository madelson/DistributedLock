using MongoDB.Driver;
using System.Collections.Concurrent;

namespace Medallion.Threading.MongoDB;

internal class MongoIndexInitializer
{
    private const string IndexName = "expiresAt_ttl";

    // We want to ensure indexes are created at most once per process per (database, collection)
    private readonly ConcurrentDictionary<(int SettingsHash, CollectionNamespace Namespace), Lazy<Task<bool?>>> _indexInitializationTasks = [];

    /// <summary>
    /// Idempotently creates a best-effort TTL index to clean up expired rows over time.
    /// Note: TTL monitors run on a schedule; correctness MUST NOT depend on this index existing.
    /// </summary>
    public Task InitializeTtlIndex(IMongoCollection<MongoLockDocument> collection)
    {
        // Include the hash code of the settings to differentiate between different clusters/clients
        // that happen to use the same database/collection names.
        // While GetHashCode() isn't perfect, it should be sufficient to distinguish between different clients/settings
        // in valid use-cases (e.g. diff connection strings).
        var clientSettingsHash = collection.Database.Client.Settings.GetHashCode();
        var key = (clientSettingsHash, collection.CollectionNamespace);

        // If we already have a task and it is in-progress or finished conclusively, noop
        if (this._indexInitializationTasks.TryGetValue(key, out var existingTask)
            && (!existingTask.Value.IsCompleted || existingTask.Value.Result.HasValue))
        {
            return existingTask.Value;
        }

        var newTask = this._indexInitializationTasks.AddOrUpdate(
            key,
            addValueFactory: static (k, a) => new(() => a.initializer.CreateIndexIfNotExistsWrapperAsync(a.collection)),
            updateValueFactory: static (k, existing, a) => existing == a.existingTask ? new(() => a.initializer.CreateIndexIfNotExistsWrapperAsync(a.collection)) : existing,
            factoryArgument: (initializer: this, collection, existingTask));
        return newTask.Value;
    }

    private async Task<bool?> CreateIndexIfNotExistsWrapperAsync(IMongoCollection<MongoLockDocument> collection)
    {
        if (await this.CreateIndexIfNotExistsAsync(collection).ConfigureAwait(false) is { } result)
        {
            return result;
        }

        // On a retryable failure, avoid resolving the task for a bit so we don't retry immediately and spam the DB
        await this.DelayBeforeRetry().ConfigureAwait(false);

        return null;
    }

    // exposed for mocking
    internal virtual Task DelayBeforeRetry() => Task.Delay(TimeSpan.FromMinutes(1));

    private async Task<bool?> CreateIndexIfNotExistsAsync(IMongoCollection<MongoLockDocument> collection)
    {
        using var activity = MongoDistributedLock.ActivitySource.StartActivity(nameof(MongoIndexInitializer) + ".CreateIndexIfNotExists");
        activity?.AddTag("collection", collection.CollectionNamespace.FullName);

        const string TagKey = "ttl_index";
        try
        {
            var indexKeys = Builders<MongoLockDocument>.IndexKeys.Ascending(d => d.ExpiresAt);
            var indexOptions = new CreateIndexOptions
            {
                // TTL cleanup: remove documents once expiresAt < now
                ExpireAfter = TimeSpan.Zero,
                Name = IndexName,
            };
            var indexModel = new CreateIndexModel<MongoLockDocument>(indexKeys, indexOptions);
            await collection.Indexes.CreateOneAsync(indexModel, cancellationToken: CancellationToken.None).ConfigureAwait(false);
            activity?.SetTag(TagKey, "created");
            return true;
        }
        catch (MongoCommandException ex) when (ex.CodeName is "IndexOptionsConflict" or "IndexKeySpecsConflict" or "IndexAlreadyExists")
        {
            // Index already exists with same or different options - this is acceptable.
            // The existing index will still handle TTL cleanup.
            activity?.SetTag(TagKey, "exists");
            return true;
        }
        catch (MongoCommandException ex) when (ex.CodeName == "Unauthorized")
        {
            try
            {
                // If we don't have permissions to create an index, we may still be able to affirm
                // that it exists by querying for it
                if (await CheckIfIndexExists(collection).ConfigureAwait(false))
                {
                    activity?.SetTag(TagKey, "exists");
                    return true;
                }
            }
            catch (Exception checkException)
            {
                activity?.SetTag("exists_check", $"failed: {checkException.GetType()}: {checkException.Message}");
            }

            activity?.SetTag(TagKey, "failed: " + ex.CodeName);
            return false; // if we're not authorized, there's no point in retrying
        }
        catch (Exception ex)
        {
            activity?.SetTag(TagKey, $"failed: {ex.GetType()}: {ex.Message}");
            activity?.SetTag("will_retry", true);
            return null; // retry ephemeral failures
        }
    }

    // exposed for testing
    internal static async Task<bool> CheckIfIndexExists(IMongoCollection<MongoLockDocument> collection)
    {
        using var cursor = await collection.Indexes.ListAsync().ConfigureAwait(false);
        while (await cursor.MoveNextAsync().ConfigureAwait(false))
        {
            foreach (var index in cursor.Current)
            {
                if (index["name"].AsString == IndexName) { return true; }

                // Check if it is a TTL index on column "expiresAt"

                // TTL indexes contain the "expireAfterSeconds" field in their options
                if (index.Contains("expireAfterSeconds"))
                {
                    var keyElement = index["key"].AsBsonDocument;
                    // Check if the first key in the index is "foo"
                    if (keyElement.Contains("expiresAt")) { return true; }
                }
            }
        }

        return false;
    }
}
