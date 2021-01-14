using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Medallion.Threading.Tests
{
    [Category("CI")]
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

            var expectedTestTypes = testCaseClasses.SelectMany(this.GetPossibleGenericInstantiations)
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
            if (NormalizeWhitespace(expectedTestContents) != NormalizeWhitespace(existingContents))
            {
                File.WriteAllText(combinatorialTestsFile, expectedTestContents);
                Assert.Fail("Updated " + combinatorialTestsFile
                    + $"**** EXPECTED **** \r\n{expectedTestContents}\r\n **** FOUND **** {existingContents ?? "NULL"}");
            }

            static string? NormalizeWhitespace(string? code) => code?.Trim().Replace("\r\n", "\n");
        }
        
        private static (string declaration, string @namespace) GetTestClassDeclaration(Type testClassType)
        {
            static string GetTestClassName(Type type)
            {
                return type.IsGenericType
                    ? $"{RemoveProviderSuffix(RemoveGenericMarkers(type.Name))}_{string.Join("_", type.GetGenericArguments().Select(GetTestClassName))}"
                    : RemoveProviderSuffix(type.Name);

                static string RemoveProviderSuffix(string name)
                {
                    var ProviderSuffix = "Provider";
                    return name.EndsWith(ProviderSuffix) ? name.Substring(0, name.Length - ProviderSuffix.Length) : name;
                }
            }

            static string GetCSharpName(Type type)
            {
                return type.IsGenericType
                    ? $"{RemoveGenericMarkers(type.Name)}<{string.Join(", ", type.GetGenericArguments().Select(GetCSharpName))}>"
                    : type.Name;
            }

            // remove words that are very common and therefore don't add much to the name
            var testClassName = Regex.Replace(GetTestClassName(testClassType), "Distributed|Lock|Testing|TestCases", string.Empty) + "Test";

            var supportsContinuousIntegrationAttributes = TraverseDepthFirst(testClassType, t => t.GetGenericArguments())
                .Where(t => t != testClassType)
                .Select(a => a.GetCustomAttribute<SupportsContinuousIntegrationAttribute>())
                .ToArray();
            var categoryAttribute = supportsContinuousIntegrationAttributes.Any(a => a == null) ? string.Empty
                : supportsContinuousIntegrationAttributes.Any(a => a!.WindowsOnly) ? "[Category(\"CIWindows\")] "
                : "[Category(\"CI\")] ";
            
            var declaration = $@"{categoryAttribute}public class {testClassName} : {GetCSharpName(testClassType)} {{ }}";

            var namespaces = TraverseDepthFirst(testClassType, t => t.GetGenericArguments())
                .Select(t => t.Namespace ?? string.Empty)
                .Distinct()
                .Where(ns => ns.StartsWith(typeof(TestSetupTest).Namespace!))
                .ToList();
            if (namespaces.Count > 1) { namespaces.RemoveAll(ns => ns == typeof(TestSetupTest).Namespace); }
            if (namespaces.Count > 1) { namespaces.RemoveAll(ns => ns.EndsWith(".Data")); }
            if (namespaces.Count > 1) { Assert.Fail(string.Join(", ", namespaces)); }
            return (declaration, namespaces.Single());
        }

        private static string RemoveGenericMarkers(string name) => Regex.Replace(name, @"`\d+", string.Empty);

        private Type[] GetPossibleGenericInstantiations(Type genericTypeDefinition)
        {
            var genericParameterTypes = genericTypeDefinition.GetGenericArguments()
                .Select(this.GetTypesForGenericParameter)
                .ToArray();
            var allCombinations = TraverseDepthFirst(
                    root: (index: 0, value: Enumerable.Empty<Type>()),
                    children: t => t.index == genericParameterTypes.Length
                        ? Enumerable.Empty<(int index, IEnumerable<Type> value)>()
                        : genericParameterTypes[t.index].Select(type => (index: t.index + 1, value: t.value.Append(type)))
                )
                .Where(t => t.index == genericParameterTypes.Length)
                .Select(t => MakeGenericTypeOrDefault(genericTypeDefinition, t.value.ToArray()))
                .Where(t => t != null).Select(t => t!)
                .ToArray();
            return allCombinations;
        }

        private Type[] GetTypesForGenericParameter(Type genericParameter)
        {
            var constraints = genericParameter.GetGenericParameterConstraints();
            return this.GetType().Assembly
                .GetTypes()
                // This doesn't support all fancy constraints like class or new()
                // see https://stackoverflow.com/questions/4864496/checking-if-an-object-meets-a-generic-parameter-constraint.
                // It also does attempt to enforce cross-constraint rules (e. g. T : Foo[V]). The idea is to identify cases 
                // that might match
                .Where(t => !t.IsNestedPrivate && !t.IsAbstract && constraints.All(c => IsDerivedFromOrDerivedFromGenericOf(derived: t, @base: c)))
                .SelectMany(t => t.IsGenericTypeDefinition ? this.GetPossibleGenericInstantiations(t) : new[] { t })
                .ToArray();
        }

        /// <summary>
        /// Attempts to construct a generic type. While we do filter down the types we try based on the generic constraints,
        /// we currently make no attempt to do cross-generic-parameter optimization such as when one generic constraint is
        /// dependent on another generic parameter (e. g. T : Foo[V]). In these cases, we fall back to the native validation
        /// </summary>
        private static Type? MakeGenericTypeOrDefault(Type genericTypeDefininition, Type[] genericArguments)
        {
            try { return genericTypeDefininition.MakeGenericType(genericArguments); }
            catch (ArgumentException) { return null; }
        }

        private static bool IsDerivedFromOrDerivedFromGenericOf(Type derived, Type @base)
        {
            if (@base.IsAssignableFrom(derived)) { return true; }
            if (!@base.IsGenericType) { return false; }

            var baseDefinition = @base.GetGenericTypeDefinition();
            return TraverseAlong(derived, t => t.BaseType)
                .Concat(derived.GetInterfaces())
                .Any(t => t.IsConstructedGenericType && t.GetGenericTypeDefinition() == baseDefinition);
        }

        // simplified versions of Traverse methods since Traverse is not strong-named

        private static IEnumerable<T> TraverseDepthFirst<T>(T root, Func<T, IEnumerable<T>> children)
        {
            yield return root;

            var stack = new Stack<IEnumerator<T>>();
            stack.Push(children(root).GetEnumerator());

            try
            {
                while (true)
                {
                    if (stack.Peek().MoveNext())
                    {
                        yield return stack.Peek().Current;
                        stack.Push(children(stack.Peek().Current).GetEnumerator());
                    }
                    else
                    {
                        stack.Peek().Dispose();
                        stack.Pop();
                        if (stack.Count == 0) { break; }
                    }
                }
            }
            finally
            {
                while (stack.Count > 0) { stack.Pop().Dispose(); }
            }
        }

        private static IEnumerable<T> TraverseAlong<T>(T? root, Func<T, T?> next)
            where T : class
        {
            for (T? node = root; node != null; node = next(node))
            {
                yield return node;
            }
        }
    }
}
