using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests
{
    public interface ITestingUpgradeableReaderWriterLockProvider : ITestingNameProvider, IDisposable
    {
        IDistributedUpgradeableReaderWriterLock CreateUpgradeableReaderWriterLockWithExactName(string name);
        string GetCrossProcessLockType(ReaderWriterLockType type);
        void PerformAdditionalCleanupForHandleAbandonment();
    }

    internal static class TestingUpgradeableReaderWriterLockProviderExtensions
    {
        /// <summary>
        /// Returns a lock whose name is based on <see cref="TestingNameProviderExtensions.GetUniqueSafeName(ITestingNameProvider, string)"/>
        /// </summary>
        public static IDistributedUpgradeableReaderWriterLock CreateLock(this ITestingUpgradeableReaderWriterLockProvider provider, string baseName) =>
            provider.CreateUpgradeableReaderWriterLockWithExactName(provider.GetUniqueSafeName(baseName));
    }
}
