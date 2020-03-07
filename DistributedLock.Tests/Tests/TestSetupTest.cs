using Medallion.Collections;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Medallion.Threading.Tests
{
    public class TestSetupTest
    {
        [Test]
        public void VerifyAllTestsAreCreated()
        {
            var testCaseClasses = this.GetType().Assembly
                .GetTypes()
                .Where(
                    t => t.IsAbstract 
                        && t.IsClass 
                        && t.IsGenericTypeDefinition 
                        && t.GetMethods().Any(m => m.GetCustomAttributes(inherit: false).Any(a => a is TestAttribute))
                )
                .ToArray();

            var expectedTestTypes = testCaseClasses.SelectMany(
                    t => this.GetTypesForGenericParameters(t.GetGenericArguments()),
                    (t, args) => t.MakeGenericType(args)
                )
                .ToArray();

            var combinatorialTestsFile = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "Tests", "CombinatorialTests.cs"));

            var expectedTestContents =
$@"using Medallion.Threading.Tests.Data;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

{string.Join(
    Environment.NewLine + Environment.NewLine,
    expectedTestTypes.Select(GetTestClassDeclaration)
        .GroupBy(t => t.@namespace, t => t.declaration)
        .OrderBy(g => g.Key)
        .Select(g =>
$@"namespace {g.Key}
{{
{string.Join(Environment.NewLine, g.OrderBy(s => s).Select(s => "    " + s))}
}}")
)}";

            var existingContents = File.Exists(combinatorialTestsFile) ? File.ReadAllText(combinatorialTestsFile) : null;
            if (expectedTestContents != existingContents)
            {
                File.WriteAllText(combinatorialTestsFile, expectedTestContents);
                Assert.Fail("Updated " + combinatorialTestsFile);
            }
        }
        
        private static (string declaration, string @namespace) GetTestClassDeclaration(Type testClassType)
        {
            static string GetTestClassName(Type type)
            {
                return type.IsGenericType
                    ? $"{RemoveGenericMarkers(type.Name)}_{string.Join("_", type.GetGenericArguments().Select(GetTestClassName))}"
                    : type.Name;
            }

            static string GetCSharpName(Type type)
            {
                return type.IsGenericType
                    ? $"{RemoveGenericMarkers(type.Name) }<{string.Join(", ", type.GetGenericArguments().Select(GetCSharpName))}>"
                    : type.Name;
            }

            // remove words that are very common and therefore don't add much to the name
            var testClassName = Regex.Replace(GetTestClassName(testClassType), "Distributed|Lock|Testing|TestCases", string.Empty) + "Test";

            var declaration = $@"public class {testClassName} : {GetCSharpName(testClassType)} {{ }}";

            var namespaces = Traverse.DepthFirst(testClassType, t => t.GetGenericArguments())
                .Select(t => t.Namespace ?? string.Empty)
                .Distinct()
                .Where(ns => ns.StartsWith(typeof(TestSetupTest).Namespace!))
                .ToList();
            if (namespaces.Count > 1) { namespaces.RemoveAll(ns => ns == typeof(TestSetupTest).Namespace); }
            if (namespaces.Count > 1) { namespaces.RemoveAll(ns => ns.EndsWith(".Data")); }
            return (declaration, namespaces.Single());
        }

        private static string RemoveGenericMarkers(string name) => Regex.Replace(name, @"`\d+", string.Empty);

        private List<Type[]> GetTypesForGenericParameters(Type[] genericParameters)
        {
            var genericParameterTypes = genericParameters.Select(this.GetTypesForGenericParameter).ToArray();
            var allCombinations = Traverse.DepthFirst(
                    root: (index: 0, value: Enumerable.Empty<Type>()),
                    children: t => t.index == genericParameterTypes.Length
                        ? Enumerable.Empty<(int index, IEnumerable<Type> value)>()
                        : genericParameterTypes[t.index].Select(type => (index: t.index + 1, value: t.value.Append(type)))
                )
                .Where(t => t.index == genericParameterTypes.Length)
                .Select(t => t.value.ToArray())
                .ToList();
            return allCombinations;
        }

        private Type[] GetTypesForGenericParameter(Type genericParameter)
        {
            var constraints = genericParameter.GetGenericParameterConstraints();
            return this.GetType().Assembly
                .GetTypes()
                // this doesn't support all fancy constraints like class or new()
                // see https://stackoverflow.com/questions/4864496/checking-if-an-object-meets-a-generic-parameter-constraint
                .Where(t => !t.IsAbstract && constraints.All(c => c.IsAssignableFrom(t)))
                .SelectMany(t => t.IsGenericTypeDefinition ? this.GetTypesForGenericParameters(t.GetGenericArguments()).Select(p => t.MakeGenericType(p)) : new[] { t })
                .ToArray();
        }
    }
}
