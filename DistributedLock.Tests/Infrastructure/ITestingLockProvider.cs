using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests
{
    public interface ITestingLockProvider : ITestingNameProvider, IDisposable
    {
        string CrossProcessLockType { get; }
        IDistributedLock CreateLockWithExactName(string name);
        void PerformAdditionalCleanupForHandleAbandonment();
    }

    internal static class TestingLockProviderExtensions
    {
        /// <summary>
        /// Returns a lock whose name is based on <see cref="TestingNameProviderExtensions.GetUniqueSafeName(ITestingNameProvider, string)"/>
        /// </summary>
        public static IDistributedLock CreateLock(this ITestingLockProvider provider, string baseName) =>
            provider.CreateLockWithExactName(provider.GetUniqueSafeName(baseName));
    }
}
