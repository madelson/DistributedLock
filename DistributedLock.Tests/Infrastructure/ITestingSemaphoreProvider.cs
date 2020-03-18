using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests
{
    public interface ITestingSemaphoreProvider : ITestingNameProvider, IDisposable
    {
        Threading.SqlServer.SqlDistributedSemaphore CreateSemaphoreWithExactName(string name, int maxCount);
        void PerformAdditionalCleanupForHandleAbandonment() { }
    }

    internal static class TestingSemaphoreProviderExtensions
    {
        /// <summary>
        /// Returns a semaphore whose name is based on <see cref="TestingNameProviderExtensions.GetUniqueSafeName(ITestingNameProvider, string)"/>
        /// </summary>
        public static Threading.SqlServer.SqlDistributedSemaphore CreateSemaphore(this ITestingSemaphoreProvider provider, string baseName, int maxCount) =>
            provider.CreateSemaphoreWithExactName(provider.GetUniqueSafeName(baseName), maxCount);
    }
}
