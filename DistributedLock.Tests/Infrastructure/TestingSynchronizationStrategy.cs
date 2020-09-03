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
        /// <summary>
        /// Whether or not abandoning a ticket held in another process will cause that ticket
        /// to be released if tickets are still held elsewhere
        /// </summary>
        public virtual bool SupportsCrossProcessSingleSemaphoreTicketAbandonment => true;

        public virtual void PrepareForHandleAbandonment() { }
        public virtual void PerformAdditionalCleanupForHandleAbandonment() { }
        public virtual IDisposable? PrepareForHandleLost() => null;
        public virtual void PrepareForHighContention() { }
        public virtual void Dispose() { }
    }
}
