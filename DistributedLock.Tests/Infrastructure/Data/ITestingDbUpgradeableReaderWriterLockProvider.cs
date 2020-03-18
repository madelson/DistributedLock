using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests.Data
{
    public interface ITestingDbUpgradeableReaderWriterLockProvider<TDbProvider>
        where TDbProvider : ITestingDbProvider
    {
        IDistributedUpgradeableReaderWriterLock CreateLockWithExactName(string name, ConnectionStrategy<TDbProvider> connectionStrategy);
        string GetSafeName(string name);
    }

    public sealed class TestingDbUpgradeableReaderWriterLockProvider<TDbProvider, TConnectionStrategyProvider, TDbLockProvider> : ITestingUpgradeableReaderWriterLockProvider
        where TDbProvider : ITestingDbProvider
        where TConnectionStrategyProvider : ITestingDbConnectionStrategyProvider<TDbProvider>, new()
        where TDbLockProvider : ITestingDbUpgradeableReaderWriterLockProvider<TDbProvider>, new()
    {
        private readonly TConnectionStrategyProvider _connectionStrategyProvider = new TConnectionStrategyProvider();

        IDistributedUpgradeableReaderWriterLock ITestingUpgradeableReaderWriterLockProvider.CreateUpgradeableReaderWriterLockWithExactName(string name) =>
            new TDbLockProvider().CreateLockWithExactName(name, this._connectionStrategyProvider.GetConnectionStrategy());

        string ITestingUpgradeableReaderWriterLockProvider.GetCrossProcessLockType(ReaderWriterLockType type) =>
            this.CreateLock(string.Empty).GetType().Name;

        void ITestingUpgradeableReaderWriterLockProvider.PerformAdditionalCleanupForHandleAbandonment() =>
            this._connectionStrategyProvider.PerformAdditionalCleanupForHandleAbandonment();

        string ITestingNameProvider.GetSafeName(string name) => new TDbLockProvider().GetSafeName(name);

        void IDisposable.Dispose() => this._connectionStrategyProvider.Dispose();
    }
}
