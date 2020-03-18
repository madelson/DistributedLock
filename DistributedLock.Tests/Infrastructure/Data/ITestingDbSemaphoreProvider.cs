using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests.Data
{
    public interface ITestingDbSemaphoreProvider<TDbProvider>
        where TDbProvider : ITestingDbProvider
    {
        Threading.SqlServer.SqlDistributedSemaphore CreateSemaphoreWithExactName(string name, int maxCount, ConnectionStrategy<TDbProvider> connectionStrategy);
        string GetSafeName(string name);
    }

    public sealed class TestingDbSemaphoreProvider<TDbProvider, TConnectionStrategyProvider, TDbSemaphoreProvider> : ITestingSemaphoreProvider
        where TDbProvider : ITestingDbProvider
        where TConnectionStrategyProvider : ITestingDbConnectionStrategyProvider<TDbProvider>, new()
        where TDbSemaphoreProvider : ITestingDbSemaphoreProvider<TDbProvider>, new()
    {
        private readonly TConnectionStrategyProvider _connectionStrategyProvider = new TConnectionStrategyProvider();

        Threading.SqlServer.SqlDistributedSemaphore ITestingSemaphoreProvider.CreateSemaphoreWithExactName(string name, int maxCount) =>
            new TDbSemaphoreProvider().CreateSemaphoreWithExactName(name, maxCount, this._connectionStrategyProvider.GetConnectionStrategy());

        void ITestingSemaphoreProvider.PerformAdditionalCleanupForHandleAbandonment() =>
            this._connectionStrategyProvider.PerformAdditionalCleanupForHandleAbandonment();

        string ITestingNameProvider.GetSafeName(string name) => new TDbSemaphoreProvider().GetSafeName(name);

        void IDisposable.Dispose() => this._connectionStrategyProvider.Dispose();
    }
}
