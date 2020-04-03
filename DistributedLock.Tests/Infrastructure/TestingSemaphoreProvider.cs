using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests
{
    public abstract class TestingSemaphoreProvider<TStrategy> : ITestingNameProvider, IDisposable
        where TStrategy : TestingSynchronizationStrategy, new()
    {
        public TStrategy Strategy { get; } = new TStrategy();

        public abstract Threading.SqlServer.SqlDistributedSemaphore CreateSemaphoreWithExactName(string name, int maxCount);
        public abstract string GetSafeName(string name);

        public virtual string GetCrossProcessLockType() =>
            this.CreateSemaphoreWithExactName(string.Empty, maxCount: 1).GetType().Name;

        /// <summary>
        /// Returns a semaphore whose name is based on <see cref="TestingNameProviderExtensions.GetUniqueSafeName(ITestingNameProvider, string)"/>
        /// </summary>
        public Threading.SqlServer.SqlDistributedSemaphore CreateSemaphore(string baseName, int maxCount) =>
            this.CreateSemaphoreWithExactName(this.GetUniqueSafeName(baseName), maxCount);

        public void Dispose() => this.Strategy.Dispose();
    }
}
