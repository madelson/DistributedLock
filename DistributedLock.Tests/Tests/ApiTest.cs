using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Tests
{
    [Category("CI")]
    public class ApiTest
    {
        private static object[] DistributedLockAssemblies => typeof(ApiTest).Assembly
            .GetReferencedAssemblies()
            .Where(a => a.Name!.StartsWith("DistributedLock."))
            .ToArray<object>();

        [TestCaseSource(nameof(DistributedLockAssemblies))]
        public void TestPublicNamespaces(AssemblyName assemblyName)
        {
            var expectedNamespace = assemblyName.Name!.Replace("DistributedLock", "Medallion.Threading")
                .Replace(".Core", string.Empty);
            foreach (var type in GetPublicTypes(Assembly.Load(assemblyName)))
            {
                type.Namespace.ShouldEqual(expectedNamespace, $"{type} in {assemblyName}");
            }
        }

        [TestCaseSource(nameof(DistributedLockAssemblies))]
        public void TestPublicApisAreSealed(AssemblyName assemblyName)
        {
            foreach (var type in GetPublicTypes(Assembly.Load(assemblyName)).Where(t => t.IsClass))
            {
                if (!type.IsAbstract)
                {
                    Assert.IsTrue(type.IsSealed, $"{type} should be sealed");
                }
                else
                {
                    Assert.IsEmpty(
                        type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            .Where(c => c.IsPublic || c.Attributes.HasFlag(MethodAttributes.Family))
                    );
                }
            }
        }

        private static IEnumerable<Type> GetPublicTypes(Assembly assembly) => assembly.GetTypes()
#if DEBUG
                .Where(t => !(t.Namespace!.Contains(".Internal") && assembly.GetName().Name == "DistributedLock.Core"))
#endif
                .Where(t => t.IsPublic || t.IsNestedPublic);
    }
}
