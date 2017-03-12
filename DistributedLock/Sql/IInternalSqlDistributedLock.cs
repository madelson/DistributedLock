using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    /// <summary>
    /// There are several strategies for implementing SQL-based locks; this interface
    /// abstracts between them to keep the implementation of <see cref="SqlDistributedLock"/> manageable
    /// </summary>
    internal interface IInternalSqlDistributedLock
    {
        // the contextHandle argument to these methods is used when acquiring a nested lock, such as upgrading
        // from an upgradeable read lock to a write lock. This allows the implementation to use the same connection
        // for the nested lock

        IDisposable TryAcquire(int timeoutMillis, SqlApplicationLock.Mode mode, IDisposable contextHandle);
        Task<IDisposable> TryAcquireAsync(int timeoutMillis, SqlApplicationLock.Mode mode, CancellationToken cancellationToken, IDisposable contextHandle);
    }
}
