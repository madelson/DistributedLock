using Medallion.Threading.Internal;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Medallion.Threading.MongoDB;

/// <summary>
/// Implements <see cref="IDistributedSynchronizationHandle" /> for <see cref="MongoDistributedLock" />
/// </summary>
public sealed class MongoDistributedLockHandle : IDistributedSynchronizationHandle
{
    private readonly CancellationTokenSource _cts;
    private readonly IMongoCollection<MongoLockDocument> _collection;
    private readonly Task _extensionTask;
    private readonly string _key;
    private readonly string _lockId;
    private int _disposed;

    /// <summary>
    /// Implements <see cref="IDistributedSynchronizationHandle.HandleLostToken" />
    /// </summary>
    public CancellationToken HandleLostToken => this._cts.Token;

    internal MongoDistributedLockHandle(
        IMongoCollection<MongoLockDocument> collection,
        string key,
        string lockId,
        TimeoutValue expiry,
        TimeoutValue extensionCadence)
    {
        this._collection = collection;
        this._key = key;
        this._lockId = lockId;
        this._cts = new();

        // Start background task to extend the lock
        this._extensionTask = this.ExtendLockAsync(expiry, extensionCadence, this._cts.Token);
    }

    /// <summary>
    /// Releases the lock
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref this._disposed, 1) is not 0)
        {
            return;
        }

        this._cts.Cancel();
        try
        {
            // Do not use HandleLostToken here: it is backed by _cts and has been canceled above.
            this._extensionTask.GetAwaiter().GetResult();
        }
        catch
        {
            // Ignore exceptions during cleanup
        }
        finally
        {
            this._cts.Dispose();
            try
            {
                this.ReleaseLockAsync(CancellationToken.None).AsTask().GetAwaiter().GetResult();
            }
            catch
            {
                // Ignore errors during release
            }
        }
    }

    /// <summary>
    /// Releases the lock asynchronously
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref this._disposed, 1) is 0)
        {
#if NET8_0_OR_GREATER
            await this._cts.CancelAsync().ConfigureAwait(false);
#else
            this._cts.Cancel();
#endif
            try
            {
                await this._extensionTask.ConfigureAwait(false);
            }
            catch
            {
                // Ignore exceptions during cleanup
            }
            finally
            {
                this._cts.Dispose();
                await this.ReleaseLockAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    private async Task ExtendLockAsync(TimeoutValue expiry, TimeoutValue extensionCadence, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(extensionCadence.TimeSpan, cancellationToken).ConfigureAwait(false);
                var filter = Builders<MongoLockDocument>.Filter.Eq(d => d.Id, this._key) & Builders<MongoLockDocument>.Filter.Eq(d => d.LockId, this._lockId);

                // Use server time ($$NOW) for expiry to avoid client clock skew.
                var newExpiresAt = new BsonDocument(
                    "$dateAdd",
                    new BsonDocument
                    {
                        { "startDate", "$$NOW" },
                        { "unit", "millisecond" },
                        { "amount", expiry.InMilliseconds }
                    }
                );
                var update = new PipelineUpdateDefinition<MongoLockDocument>(
                    new[] { new BsonDocument("$set", new BsonDocument("expiresAt", newExpiresAt)) }
                );

                var result = await this._collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);

                // If we failed to extend, the lock was lost
                if (result.MatchedCount is not 0)
                {
                    continue;
                }
#if NET8_0_OR_GREATER
                await this._cts.CancelAsync().ConfigureAwait(false);
#else
                this._cts.Cancel();
#endif
                break;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when disposing
        }
        catch
        {
            // Lock extension failed, signal that the lock is lost
#if NET8_0_OR_GREATER
            await this._cts.CancelAsync().ConfigureAwait(false);
#else
            this._cts.Cancel();
#endif
        }
    }

    private async ValueTask ReleaseLockAsync(CancellationToken cancellationToken)
    {
        try
        {
            var filter = Builders<MongoLockDocument>.Filter.Eq(d => d.Id, this._key) & Builders<MongoLockDocument>.Filter.Eq(d => d.LockId, this._lockId);
            await this._collection.DeleteOneAsync(filter, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Ignore errors during release
        }
    }
}