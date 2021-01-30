# Reader-writer locks

DistributedLock's implementations of [reader-writer locks](https://en.wikipedia.org/wiki/Readers%E2%80%93writer_lock) are similar to the framework's non-distributed [ReaderWriterLockSlim](https://msdn.microsoft.com/en-us/library/system.threading.readerwriterlockslim(v=vs.110).aspx) class.

## Basics

A reader-writer lock allows for *EITHER multiple readers OR one writer* to hold the lock at any given time. This is useful for protecting resources that are normally safe for concurrent access but need to sometimes be locked, such as when changes are being made. For example, a distributed reader-writer lock could be used to provide thread-safety in a distributed cache:

```C#
class DistributedCache
{
    // uses the SQLServer implementation, but others are available as well
    private readonly SqlDistributedReaderWriterLock _cacheLock = 
        new SqlDistributedReaderWriterLock("DistributedCache", connectionString);
        
    /// <summary>
    /// If key is present in the cache, returns the associated value. If key is not present, generates a new
    /// value with the provided valueFactory, stores that value in the cache, and returns the generated value.
    /// </summary>
    public async Task<object> GetOrCreateAsync(string key, Func<string, object> valueFactory)
    {
        // first, take the read lock to avoid blocking the cache in the case of a cache hit
        await using (await this._cacheLock.AcquireReadLockAsync())
        {
            var cached = await this.GetValueOrDefaultNoLockAsync(key);
            if (cached != null) { return cached; } // cache hit
        }
        
        // seems like we'll need to write to the cache; take the write lock
        await using (await this._cacheLock.AcquireWriteLockAsync())
        {
            // double-check: the value might have been written by another process 
            // while we were waiting to get the write lock
            var cached = await this.GetValueOrDefaultNoLockAsync(key);
            if (cached != null) { return cached; } // cache hit
            
            var generated = valueFactory(key);
            await this.SetValueAsync(key, generated);
            return generated;
        }
    }
    
    private async Task<object?> GetValueOrDefaultNoLockAsync(string key) { /* reads from underlying storage */ }
    
    private async Task SetValueAsync(string key, object value) { /* writes to underlying storage */ }
}
```

This approach is more efficient than simply wrapping the entire operation in a regular distributed lock because cache hits don't block each other.

## Upgradeable reader-writer locks

Some reader-writer lock implementations further support acquiring an *upgradeable read* lock. When acquired, this lock blocks other writers and upgradeable readers but does not block other readers. Furthermore, an upgradeable read lock can be upgraded to a write lock without having to be released first (with a regular read lock, you must release it before acquiring a write lock.

It may seem tempting to use an upgradeable read lock instead of both a read lock and a write lock in the cache scenario describe above, but the [problem with this](https://ayende.com/blog/4349/using-readerwriterlockslims-enterupgradeablereadlock) is that it would only allow one caller inside GetOrCreate at any giving time. 

In some cases, though, it is useful to be able to block writers for a time without blocking readers. Consider the following example of a checkout system where we want to protect modification of the shopping cart data model with a lock:

```
class ShoppingCartService
{
    public ShoppingCartDetails GetDetails(Guid cartId)
    {
        using (this.GetCartLock(cartId).AcquireReadLock())
        {
            // read from cart data model
        }
    }
    
    public void Checkout(Guid cartId)
    {
        using var handle = this.GetCartLock(cartId).AcquireUpgradeableReadLock();
        
        // This makes some API calls to other systems and can be slow. We want an upgradeable 
        // read lock because we don't want to call Submit() multiple times for the same cart, but we
        // don't need to block readers of the cart data model because we're not editing it
        var submissionInfo = SubmitOrder(cartId);
        
        // now it's time to edit the cart data model, so upgrade to a write lock
        handle.UpgradeToWriteLock();
        
        // write to cart data model
    }
    
    private SqlDistributedReaderWriterLock GetCartLock(Guid cartId) =>
        new SqlDistributedReaderWriterLock("Cart_" + cartId, connectionString);
}
```
