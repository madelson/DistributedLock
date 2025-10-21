using Medallion.Threading.Internal;
using MongoDB.Driver;

namespace Medallion.Threading.MongoDB;

/// <summary>
/// Implements <see cref="IDistributedSynchronizationHandle" /> for <see cref="MongoDistributedLock" />
/// </summary>
public sealed class MongoDistributedLockHandle : IDistributedSynchronizationHandle
{
    private readonly string _collectionName;
    private readonly CancellationTokenSource _cts;
    private readonly IMongoDatabase _database;
    private readonly Task _extensionTask;
    private readonly string _key;
    private readonly string _lockId;
    private int _disposed;

    /// <summary>
    /// Implements <see cref="IDistributedSynchronizationHandle.HandleLostToken" />
    /// </summary>
    public CancellationToken HandleLostToken => _cts.Token;

    internal MongoDistributedLockHandle(
        IMongoDatabase database,
        string collectionName,
        string key,
        string lockId,
        TimeoutValue expiry,
        TimeoutValue extensionCadence)
    {
        _database = database;
        _collectionName = collectionName;
        _key = key;
        _lockId = lockId;
        _cts = new();

        // Start background task to extend the lock
        _extensionTask = ExtendLockAsync(expiry, extensionCadence, _cts.Token);
    }

    /// <summary>
    /// Releases the lock
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) is not 0)
        {
            return;
        }
        _cts.Cancel();
        try
        {
            _extensionTask.Wait(HandleLostToken);
        }
        catch
        {
            // Ignore exceptions during cleanup
        }
        finally
        {
            _cts.Dispose();
            ReleaseLockAsync(CancellationToken.None).AsTask().Wait(HandleLostToken);
        }
    }

    /// <summary>
    /// Releases the lock asynchronously
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) is 0)
        {
            _cts.Cancel();
            try
            {
                await _extensionTask.ConfigureAwait(false);
            }
            catch
            {
                // Ignore exceptions during cleanup
            }
            finally
            {
                _cts.Dispose();
                await ReleaseLockAsync(CancellationToken.None).ConfigureAwait(false);
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
                var collection = _database.GetCollection<MongoLockDocument>(_collectionName);
                var filter = Builders<MongoLockDocument>.Filter.Eq(d => d.Id, _key) & Builders<MongoLockDocument>.Filter.Eq(d => d.LockId, _lockId);
                var update = Builders<MongoLockDocument>.Update.Set(d => d.ExpiresAt, DateTime.UtcNow.Add(expiry.TimeSpan));
                var result = await collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);

                // If we failed to extend, the lock was lost
                if (result.MatchedCount is not 0)
                {
                    continue;
                }
                _cts.Cancel();
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
            _cts.Cancel();
        }
    }

    private async ValueTask ReleaseLockAsync(CancellationToken cancellationToken)
    {
        try
        {
            var collection = _database.GetCollection<MongoLockDocument>(_collectionName);
            var filter = Builders<MongoLockDocument>.Filter.Eq(d => d.Id, _key) & Builders<MongoLockDocument>.Filter.Eq(d => d.LockId, _lockId);
            await collection.DeleteOneAsync(filter, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Ignore errors during release
        }
    }
}