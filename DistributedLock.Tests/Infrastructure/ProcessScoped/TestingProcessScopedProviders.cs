using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests.ProcessScoped
{
    [SupportsContinuousIntegration]
    public sealed class TestingProcessScopedNamedLockProvider : TestingLockProvider<TestingProcessScopedSynchronizationStrategy>
    {
        public override IDistributedLock CreateLockWithExactName(string name) => new ProcessScopedNamedLock(name);

        public override string GetSafeName(string name) => new ProcessScopedNamedLock(name).Name;
    }

    [SupportsContinuousIntegration]
    public sealed class TestingProcessScopedNamedSemaphoreProvider : TestingSemaphoreProvider<TestingProcessScopedSynchronizationStrategy>
    {
        public override IDistributedSemaphore CreateSemaphoreWithExactName(string name, int maxCount) => new ProcessScopedNamedSemaphore(name, maxCount);

        public override string GetSafeName(string name) => new ProcessScopedNamedSemaphore(name, 1).Name;
    }

    [SupportsContinuousIntegration]
    public sealed class TestingProcessScopedNamedReaderWriterLockProvider : TestingUpgradeableReaderWriterLockProvider<TestingProcessScopedSynchronizationStrategy>
    {
        public override IDistributedUpgradeableReaderWriterLock CreateUpgradeableReaderWriterLockWithExactName(string name) =>
            new ProcessScopedNamedReaderWriterLock(name);

        public override string GetSafeName(string name) => new ProcessScopedNamedReaderWriterLock(name).Name;
    }
}
