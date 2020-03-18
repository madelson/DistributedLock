using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests.Data
{
    public interface ITestingDbReaderWriterLockProvider<TDbProvider>
        where TDbProvider : ITestingDbProvider
    {
        IDistributedReaderWriterLock CreateLockWithExactName(string name, ConnectionStrategy<TDbProvider> connectionStrategy);
        string GetSafeName(string name);
    }

    public sealed class TestingDbReaderWriterLockProvider<TDbProvider, TConnectionStrategyProvider, TDbLockProvider> : ITestingReaderWriterLockProvider
        where TDbProvider : ITestingDbProvider
        where TConnectionStrategyProvider : ITestingDbConnectionStrategyProvider<TDbProvider>, new()
        where TDbLockProvider : ITestingDbReaderWriterLockProvider<TDbProvider>, new()
    {
        private readonly TConnectionStrategyProvider _connectionStrategyProvider = new TConnectionStrategyProvider();

        IDistributedReaderWriterLock ITestingReaderWriterLockProvider.CreateReaderWriterLockWithExactName(string name) =>
            new TDbLockProvider().CreateLockWithExactName(name, this._connectionStrategyProvider.GetConnectionStrategy());

        string ITestingReaderWriterLockProvider.GetCrossProcessLockType(ReaderWriterLockType type) =>
            this.CreateLock(string.Empty).GetType().Name;

        void ITestingReaderWriterLockProvider.PerformAdditionalCleanupForHandleAbandonment() =>
            this._connectionStrategyProvider.PerformAdditionalCleanupForHandleAbandonment();

        string ITestingNameProvider.GetSafeName(string name) => new TDbLockProvider().GetSafeName(name);

        void IDisposable.Dispose() => this._connectionStrategyProvider.Dispose();
    }
}
