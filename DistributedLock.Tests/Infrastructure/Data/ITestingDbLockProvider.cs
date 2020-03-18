using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests.Data
{
    public interface ITestingDbLockProvider<TDbProvider>
        where TDbProvider : ITestingDbProvider
    {
        IDistributedLock CreateLockWithExactName(string name, ConnectionStrategy<TDbProvider> connectionStrategy);
        string GetSafeName(string name);
    }

    public sealed class TestingDbLockProvider<TDbProvider, TConnectionStrategyProvider, TDbLockProvider> : ITestingLockProvider
        where TDbProvider : ITestingDbProvider
        where TConnectionStrategyProvider : ITestingDbConnectionStrategyProvider<TDbProvider>, new()
        where TDbLockProvider : ITestingDbLockProvider<TDbProvider>, new()
    {
        private readonly TConnectionStrategyProvider _connectionStrategyProvider = new TConnectionStrategyProvider();

        string ITestingLockProvider.CrossProcessLockType => this.CreateLock(string.Empty).GetType().Name;

        IDistributedLock ITestingLockProvider.CreateLockWithExactName(string name) =>
            new TDbLockProvider().CreateLockWithExactName(name, this._connectionStrategyProvider.GetConnectionStrategy());

        string ITestingNameProvider.GetSafeName(string name) => new TDbLockProvider().GetSafeName(name);

        void ITestingLockProvider.PerformAdditionalCleanupForHandleAbandonment() => 
            this._connectionStrategyProvider.PerformAdditionalCleanupForHandleAbandonment();

        void IDisposable.Dispose() => this._connectionStrategyProvider.Dispose();
    }
}
