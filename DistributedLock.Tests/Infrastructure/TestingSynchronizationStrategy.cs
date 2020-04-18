using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests
{
    /// <summary>
    /// Manages the underlying approach to synchronization. Having this class allows us to parameterize tests by
    /// synchronization strategy (e. g. only connection string-based strategies)
    /// </summary>
    public abstract class TestingSynchronizationStrategy : IDisposable
    {
        public virtual void PrepareForHandleAbandonment() { }
        public virtual void PerformAdditionalCleanupForHandleAbandonment() { }
        public virtual IDisposable? PrepareForHandleLost() => null;
        public virtual void PrepareForHighContention() { }
        public virtual void Dispose() { }
    }
}
