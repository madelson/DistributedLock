using System;

namespace Medallion.Threading.Sql
{
    /// <summary>
    /// There are several strategies for implementing SQL-based locks; this interface
    /// abstracts between them to keep the implementation of <see cref="SqlDistributedLock"/> manageable
    /// </summary>
    internal interface IInternalSqlDistributedLocks
    {
        // the contextHandle argument to these methods is used when acquiring a nested lock, such as upgrading
        // from an upgradeable read lock to a write lock. This allows the implementation to use the same connection
        // for the nested lock

        IDisposable? TryAcquire<TLockCookie>(int timeoutMillis, ISqlSynchronizationStrategyMultiple<TLockCookie> strategy, IDisposable? contextHandle) where TLockCookie : class;
    }
}
