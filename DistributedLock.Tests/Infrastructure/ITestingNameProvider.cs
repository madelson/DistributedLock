using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests
{
    public interface ITestingNameProvider
    {
        string GetSafeName(string name);
    }

    internal static class TestingNameProviderExtensions
    {
        /// <summary>
        /// Returns a name based on <paramref name="baseName"/> which is "namespaced" by the current
        /// test and framework name, thus avoiding potential collisions between test cases
        /// </summary>
        public static string GetUniqueSafeName(this ITestingNameProvider provider, string baseName = "") =>
            provider.GetSafeName($"{baseName}_{TestHelper.UniqueName}");
    }
}
