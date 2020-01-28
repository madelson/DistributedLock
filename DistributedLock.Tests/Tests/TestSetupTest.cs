using Medallion.Collections;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

            var testClasses = this.GetType().Assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && t.Name.EndsWith("Test"))
                .ToArray();

            var expectedTestTypes = testCaseClasses.SelectMany(
                    t => this.GetTypesForGenericParameters(t.GetGenericArguments()),
                    (t, args) => t.MakeGenericType(args)
                )
                .ToArray();
            var missing = expectedTestTypes.Where(t => !testClasses.Any(c => t.IsAssignableFrom(c)))
                .Select(GetTestClassDeclaration)
                .ToList();

            if (missing.Any())
            {
                File.WriteAllText(
                    Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "Tests", "CombinatorialTests.cs"),
$@"using Medallion.Threading.Tests.Sql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests
{{
    // AUTO-GENERATED
    // Contains test classes which implement abstract test cases in all valid combinations. Tests missing from here are discovered by TestSetupTest
{string.Join(string.Empty, expectedTestTypes.Select(GetTestClassDeclaration).OrderBy(s => s))}
}}"
                );
            }
            missing.Count.ShouldEqual(0, "Missing: " + string.Join(string.Empty, missing));
        }
        
        private static string GetTestClassDeclaration(Type testClassType)
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

            return $@"
    public class {testClassName} : {GetCSharpName(testClassType)} {{ }}";
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
