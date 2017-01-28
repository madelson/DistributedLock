using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql.ConnectionMultiplexing
{
    internal static class MultiplexedConnectionLockPool
    {
        private static readonly Hashtable WeakPooledConnectionLocksByConnectionString = new Hashtable();
        private static int writesUntilNextPurge = 1;

        public static MultiplexedConnectionLock Get(string connectionString)
        {
            WeakReference<MultiplexedConnectionLock> existingReference;
            var existing = TryGet(connectionString, out existingReference);
            if (existing != null) { return existing; }

            lock (WeakPooledConnectionLocksByConnectionString.SyncRoot)
            {
                existing = TryGet(connectionString, out existingReference);
                if (existing != null) { return existing; }

                var @lock = new MultiplexedConnectionLock(connectionString);

                // if we have a dead reference, then just point it at a new lock. Since we
                // aren't growing the dictionary, we don't need to consider purging
                if (existingReference != null)
                {
                    existingReference.SetTarget(@lock);
                    return @lock;
                }

                --writesUntilNextPurge;
                if (writesUntilNextPurge <= 0)
                {
                    List<object> keysToRemove = null;
                    foreach (DictionaryEntry entry in WeakPooledConnectionLocksByConnectionString)
                    {
                        MultiplexedConnectionLock target;
                        if (!((WeakReference<MultiplexedConnectionLock>)entry.Value).TryGetTarget(out target))
                        {
                            (keysToRemove ?? (keysToRemove = new List<object>())).Add(entry.Key);
                        }
                    }
                    keysToRemove?.ForEach(WeakPooledConnectionLocksByConnectionString.Remove);
                    writesUntilNextPurge = Math.Max(WeakPooledConnectionLocksByConnectionString.Count, 1);
                }

                // Add() is safe because we checked for an existing reference above
                WeakPooledConnectionLocksByConnectionString.Add(connectionString, new WeakReference<MultiplexedConnectionLock>(@lock));
                return @lock;
            }
        }

        private static MultiplexedConnectionLock TryGet(string connectionString, out WeakReference<MultiplexedConnectionLock> reference)
        {
            reference = (WeakReference<MultiplexedConnectionLock>)WeakPooledConnectionLocksByConnectionString[connectionString];
            MultiplexedConnectionLock result;
            if (reference != null && reference.TryGetTarget(out result))
            {
                return result;
            }
            return null;
        }
    }
}
