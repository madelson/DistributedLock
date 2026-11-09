using System.Collections.Concurrent;

namespace Medallion.Threading.InProcess;

/// <summary>
/// A thread-safe name-keyed cache which does not keep its values alive. This allows synchronization primitives
/// to be shared by all callers using the same name while still being collected once nothing references them.
/// </summary>
internal class ConcurrentWeakReferenceDictionary<TKey, TValue>
    where TKey : notnull
    where TValue : class
{
    private const int CleanupThreshold = 1000;

    private readonly ConcurrentDictionary<TKey, WeakReference<TValue>> _dictionary = new();
    private int _operationCount;

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        var result = GetOrAddCore();

        // Opportunistic cleanup
        if (Interlocked.Increment(ref this._operationCount) >= CleanupThreshold
         // only run cleanup if we won the race in resetting the op count
         && Interlocked.Exchange(ref this._operationCount, 0) >= CleanupThreshold)
        {
            this.RemoveDeadEntries();
        }

        return result;

        TValue GetOrAddCore()
        {
            // Cached across loop iterations so that losing a race does not re-invoke valueFactory.
            TValue? newValue = null;
            WeakReference<TValue>? newReference = null;

            while (true)
            {
                if (this._dictionary.TryGetValue(key, out var existingReference))
                {
                    // fast path: a live value is already present
                    if (existingReference.TryGetTarget(out var existingValue)) { return existingValue; }

                    // The entry is dead. Replace it, but only if the slot still holds the exact dead reference
                    // we observed; otherwise another thread has already installed a live replacement.
                    newValue ??= valueFactory(key);
                    newReference ??= new WeakReference<TValue>(newValue);
                    if (this._dictionary.TryUpdate(key, newReference, comparisonValue: existingReference))
                    {
                        return newValue;
                    }
                }
                else
                {
                    newValue ??= valueFactory(key);
                    newReference ??= new WeakReference<TValue>(newValue);
                    if (this._dictionary.TryAdd(key, newReference))
                    {
                        return newValue;
                    }
                }

                // We lost a race (or our own value was collected before we could return it). Re-read and retry.
            }
        }
    }

    private void RemoveDeadEntries()
    {
        foreach (var kvp in this._dictionary)
        {
            // Only remove if the entry is STILL the exact dead reference we observed. Removing by key alone would
            // race with a concurrent GetOrAdd which may have just installed a live replacement, which would cause
            // a subsequent GetOrAdd to hand out a second primitive for a name that is already in use.
            if (!kvp.Value.TryGetTarget(out _))
            {
                Remove(this._dictionary, kvp);
            }
        }
    }

    private static void Remove<T>(ICollection<T> collection, T value) => collection.Remove(value);
}
