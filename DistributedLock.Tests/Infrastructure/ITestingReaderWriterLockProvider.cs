using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests
{
    public interface ITestingReaderWriterLockProvider : ITestingNameProvider, IDisposable
    {
        IDistributedReaderWriterLock CreateReaderWriterLockWithExactName(string name);
        string GetCrossProcessLockType(ReaderWriterLockType type);
        void PerformAdditionalCleanupForHandleAbandonment() { }
    }

    public enum ReaderWriterLockType
    {
        Read,
        Write,
        Upgrade,
    }

    internal static class TestingReaderWriterLockProviderExtensions
    {
        /// <summary>
        /// Returns a lock whose name is based on <see cref="TestingNameProviderExtensions.GetUniqueSafeName(ITestingNameProvider, string)"/>
        /// </summary>
        public static IDistributedReaderWriterLock CreateLock(this ITestingReaderWriterLockProvider provider, string baseName) =>
            provider.CreateReaderWriterLockWithExactName(provider.GetUniqueSafeName(baseName));
    }
}
