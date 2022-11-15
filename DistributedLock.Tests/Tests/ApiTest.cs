using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text.RegularExpressions;

namespace Medallion.Threading.Tests;

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

    [TestCaseSource(nameof(DistributedLockAssemblies))]
    public void TestProviderApisAreAvailable(AssemblyName assemblyName)
    {
        var providerTypesToProvidedTypes = typeof(IDistributedLockProvider).Assembly
            .GetTypes()
            .Where(t => t.IsInterface && t.IsPublic && t.Name.EndsWith("Provider"))
            .ToDictionary(t => t, t => t.GetMethods().Single(m => m.Name.StartsWith("Create")).ReturnType);

        var types = GetPublicTypes(Assembly.Load(assemblyName));

        foreach (var kvp in providerTypesToProvidedTypes)
        {
            var providers = types.Where(t => !t.IsInterface && kvp.Key.IsAssignableFrom(t)).ToArray();
            var provided = types.Where(t => !t.IsInterface && kvp.Value.IsAssignableFrom(t)).ToArray();
            CollectionAssert.AreEquivalent(
                provided, 
                providers.Select(t => t.GetMethods().Single(m => m.Name.StartsWith("Create") && kvp.Value.IsAssignableFrom(m.ReturnType)).ReturnType));

            foreach (var provider in providers)
            {
                Assert.That(provider.Name, Does.EndWith("DistributedSynchronizationProvider"));
            }
        }
    }

    [TestCaseSource(nameof(DistributedLockAssemblies))]
    public void TestPublicHandleTypesDoNotHaveVisibleConstructors(AssemblyName assemblyName)
    {
        var publicHandleTypes = GetPublicTypes(Assembly.Load(assemblyName))
            .Where(t => typeof(IDistributedSynchronizationHandle).IsAssignableFrom(t))
            .ToArray();
        Assert.IsNotEmpty(publicHandleTypes); // sanity check

        foreach (var publicHandleType in publicHandleTypes)
        {
            Assert.IsEmpty(
                publicHandleType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(c => c.IsPublic || c.IsFamily || c.IsFamilyOrAssembly)
            );
        }
    }

    [TestCaseSource(nameof(DistributedLockAssemblies))]
    public void TestLibrariesUseConfigureAwaitFalse(AssemblyName assemblyName)
    {
        var projectDirectory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(CurrentFilePath())!, "..", "..", assemblyName.Name!));
        var codeFiles = Directory.GetFiles(projectDirectory, "*.cs", SearchOption.AllDirectories);
        Assert.IsNotEmpty(codeFiles);

        var awaitRegex = new Regex(@"//.*|(?<await>\bawait\s)");
        var configureAwaitRegex = new Regex(@"\.ConfigureAwait\(false\)|\.TryAwait\(\)");
        foreach (var codeFile in codeFiles)
        {
            var code = File.ReadAllText(codeFile);
            var awaitCount = awaitRegex.Matches(code).Cast<Match>().Count(m => m.Groups["await"].Success);
            var configureAwaitCount = configureAwaitRegex.Matches(code).Count;
            Assert.IsTrue(configureAwaitCount >= awaitCount, $"ConfigureAwait(false) count ({configureAwaitCount}) < await count ({awaitCount}) in {codeFile}");
        }
    }

    [Test]
    public void TestLibraryFilesDoNotWriteToConsole()
    {
        var projectDirectory = Path.GetDirectoryName(Path.GetDirectoryName(CurrentFilePath()));
        var solutionDirectory = Path.GetDirectoryName(projectDirectory!);
        var libraryCsFiles = Directory.GetFiles(solutionDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(f => new[] { ".Tests", "CodeGen", "DistributedLockTaker" }.All(s => f.IndexOf(s, StringComparison.OrdinalIgnoreCase) < 0));
        Assert.IsEmpty(
            libraryCsFiles.Where(f => File.ReadAllText(f).Contains("Console."))
                .Select(Path.GetFileName)
        );
    }

    [TestCaseSource(nameof(DistributedLockAssemblies))]
    public void TestInternalNamedMembersAreInternal(AssemblyName assemblyName)
    {
        var assembly = Assembly.Load(assemblyName);
        var publicTypes = GetPublicTypes(assembly);
        foreach (var publicType in publicTypes)
        {
            Assert.IsEmpty(
                publicType.GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                    .Where(m => m.Name.Contains("Internal"))
            );
        }
    }

    [TestCaseSource(nameof(DistributedLockAssemblies))]
    public void TestLegacyGetSafeNameApisAreRemoved(AssemblyName assemblyName)
    {
        var assembly = Assembly.Load(assemblyName);
        var publicTypes = GetPublicTypes(assembly);
        foreach (var publicType in publicTypes)
        {
            Assert.IsNull(publicType.GetMethod("GetSafeName", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNull(publicType.GetProperty("MaxNameLength", BindingFlags.Public | BindingFlags.Static));
        }
    }

    [TestCaseSource(nameof(DistributedLockAssemblies))]
    public void TestAssemblyVersioning(AssemblyName assemblyName)
    {
        var assembly = Assembly.Load(assemblyName);
        Assert.IsNotNull(assembly.GetName().GetPublicKeyToken(), "Should be signed");

        // scheme based on https://codingforsmarties.wordpress.com/2016/01/21/how-to-version-assemblies-destined-for-nuget/
        var version = assembly.GetName().Version;
        version!.Minor.ShouldEqual(0);
        version.Revision.ShouldEqual(0);
        version.Build.ShouldEqual(0);
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        Assert.That(informationalVersion?.InformationalVersion, Does.StartWith($"{version.Major}."));
    }

    private static IEnumerable<Type> GetPublicTypes(Assembly assembly) => assembly.GetTypes()
            .Where(IsInPublicApi)
#if DEBUG
            .Where(t => !(t.Namespace!.Contains(".Internal") && assembly.GetName().Name == "DistributedLock.Core"))
#endif
        ;

    private static string CurrentFilePath([CallerFilePath] string filePath = "") => filePath;

    private static bool IsInPublicApi(Type type) => type.IsPublic
        || ((type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamORAssem) && IsInPublicApi(type.DeclaringType!));
}
