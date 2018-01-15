using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests
{
    [TestClass]
    public class TestSetupTest
    {
        [TestMethod]
        public void VerifyAllTestsAreCreated()
        {
            var testCaseClasses = this.GetType().Assembly
                .GetTypes()
                .Where(
                    t => t.IsAbstract 
                        && t.IsClass 
                        && t.IsGenericTypeDefinition 
                        && t.GetMethods().Any(m => m.GetCustomAttributes(inherit: false).Any(a => a is TestMethodAttribute))
                )
                .ToArray();

            var testClasses = this.GetType().Assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && t.GetCustomAttributes(inherit: false).Any(a => a is TestClassAttribute))
                .ToArray();

            var missing = new List<string>();
            foreach (var testCaseClass in testCaseClasses)
            {
                var possibleGenericParameterTypes = this.GetTypesForGenericParameters(testCaseClass.GetGenericArguments());
                var possibleTestTypes = possibleGenericParameterTypes.Select(p => testCaseClass.MakeGenericType(p));
                missing.AddRange(
                    possibleTestTypes.Where(t => !testClasses.Any(c => t.IsAssignableFrom(c)))
                        .Select(GetTestClassDeclaration)
                );
            }

            missing.Count.ShouldEqual(0, "Missing: " + string.Join(Environment.NewLine, missing));
        }
        
        private static string GetTestClassDeclaration(Type testClassType)
        {
            string GetTestClassName(Type type)
            {
                return type.IsGenericType
                    ? $"{RemoveGenericMarkers(type.Name)}_{string.Join("_", type.GetGenericArguments().Select(GetTestClassName))}"
                    : type.Name;
            }

            string GetCSharpName(Type type)
            {
                return type.IsGenericType
                    ? $"{RemoveGenericMarkers(type.Name) }<{string.Join(", ", type.GetGenericArguments().Select(GetCSharpName))}>"
                    : type.Name;
            }

            // remove words that are very common and therefore don't add much to the name
            var testClassName = Regex.Replace(GetTestClassName(testClassType), "Distributed|Lock|Testing|TestCases", string.Empty) + "Test";

            return $@"
    [TestClass]
    public class {testClassName} : {GetCSharpName(testClassType)} {{ }}";
        }

        private static string RemoveGenericMarkers(string name) => Regex.Replace(name, @"`\d+", string.Empty);

        private List<Type> GetTypesForGenericParameters(Type[] genericParameters)
        {
            if (genericParameters.Length > 1) { throw new NotSupportedException(); }

            var constraints = genericParameters[0].GetGenericParameterConstraints();
            return this.GetType().Assembly
                .GetTypes()
                // this doesn't support fancy constraints like class
                // see https://stackoverflow.com/questions/4864496/checking-if-an-object-meets-a-generic-parameter-constraint
                .Where(t => !t.IsAbstract && constraints.All(c => c.IsAssignableFrom(t)))
                .SelectMany(t => t.IsGenericTypeDefinition ? this.GetTypesForGenericParameters(t.GetGenericArguments()).Select(p => t.MakeGenericType(p)) : new[] { t })
                .ToList();
        }
    }
}
