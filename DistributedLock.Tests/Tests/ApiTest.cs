using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Tests
{
    public class ApiTest
    {
        private static object[] DistributedLockAssemblies => typeof(ApiTest).Assembly
            .GetReferencedAssemblies()
            .Where(a => a.Name!.StartsWith("DistributedLock."))
            .ToArray<object>();

        [TestCaseSource(nameof(DistributedLockAssemblies))]
        public void TestPublicNamespaces(AssemblyName assemblyName)
        {
            var assembly = Assembly.Load(assemblyName);
            var publicTypes = assembly.GetTypes()
#if DEBUG
                .Where(t => assemblyName.Name != "DistributedLock.Core" || !t.Namespace!.Contains(".Internal"))
#endif
                .Where(t => t.IsPublic);

            var expectedNamespace = assemblyName.Name!.Replace("DistributedLock", "Medallion.Threading")
                .Replace(".Core", string.Empty);
            foreach (var type in publicTypes)
            {
                type.Namespace.ShouldEqual(expectedNamespace, $"{type} in {assemblyName}");
            }
        }
    }
}
