using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DistributedLockCodeGen;

[Category("CI")]
public class GenerateProviders
{
    public static readonly IReadOnlyList<string> Interfaces = new[]
    {
        "IDistributedLock",
        "IDistributedReaderWriterLock",
        "IDistributedUpgradeableReaderWriterLock",
        "IDistributedSemaphore"
    };

    [TestCaseSource(nameof(Interfaces))]
    public void GenerateProviderInterfaceAndExtensions(string interfaceName)
    {
        var interfaceFile = Directory.GetFiles(CodeGenHelpers.SolutionDirectory, interfaceName + ".cs", SearchOption.AllDirectories)
            .Single();
        var providerInterfaceName = interfaceName + "Provider";

        var createMethodName = $"Create{interfaceName.Replace("IDistributed", string.Empty)}";
        var providerInterfaceCode = $@"// AUTO-GENERATED
namespace Medallion.Threading;

/// <summary>
/// Acts as a factory for <see cref=""{interfaceName}""/> instances of a certain type. This interface may be
/// easier to use than <see cref=""{interfaceName}""/> in dependency injection scenarios.
/// </summary>
public interface {providerInterfaceName}{(interfaceName == "IDistributedUpgradeableReaderWriterLock" ? ": IDistributedReaderWriterLockProvider" : string.Empty)}
{{
    /// <summary>
    /// Constructs an <see cref=""{interfaceName}""/> instance with the given <paramref name=""name""/>.
    /// </summary>
    {interfaceName} {createMethodName}(string name{(interfaceName.Contains("Semaphore") ? ", int maxCount" : string.Empty)});
}}";

        var interfaceMethods = Regex.Matches(
            File.ReadAllText(interfaceFile),
            @"(?<returnType>\S+) (?<name>\S+)\((?<parameters>((?<parameterType>\S*) (?<parameterName>\w+)[^,)]*(\, )?)*)\);",
            RegexOptions.ExplicitCapture
        );
        var extensionMethodBodies = interfaceMethods.Cast<Match>()
            .Select(m =>
$@"    /// <summary>
    /// Equivalent to calling <see cref=""{providerInterfaceName}.{createMethodName}(string{(interfaceName.Contains("Semaphore") ? ", int" : string.Empty)})"" /> and then
    /// <see cref=""{interfaceName}.{m.Groups["name"].Value}({string.Join(", ", m.Groups["parameterType"].Captures.Cast<Capture>().Select(c => c.Value))})"" />.
    /// </summary>
    public static {m.Groups["returnType"].Value} {GetExtensionMethodName(m.Groups["name"].Value)}(this {providerInterfaceName} provider, string name{(interfaceName.Contains("Semaphore") ? ", int maxCount" : string.Empty)}, {m.Groups["parameters"].Value}) =>
        (provider ?? throw new ArgumentNullException(nameof(provider))).{createMethodName}(name{(interfaceName.Contains("Semaphore") ? ", maxCount" : string.Empty)}).{m.Groups["name"].Value}({string.Join(", ", m.Groups["parameterName"].Captures.Cast<Capture>().Select(c => c.Value))});"
            );

        var providerExtensionsName = providerInterfaceName.TrimStart('I') + "Extensions";
        var providerExtensionsCode = $@"// AUTO-GENERATED
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading;

/// <summary>
/// Productivity helper methods for <see cref=""{providerInterfaceName}"" />
/// </summary>
public static class {providerExtensionsName}
{{
{string.Join(Environment.NewLine + Environment.NewLine, extensionMethodBodies)}
}}";

        var changes = new[]
            {
                (name: providerInterfaceName, code: providerInterfaceCode),
                (name: providerExtensionsName, code: providerExtensionsCode)
            }
            .Select(t => (file: Path.Combine(Path.GetDirectoryName(interfaceFile)!, t.name + ".cs"), t.code))
            .Select(t => (t.file, t.code, originalCode: File.Exists(t.file) ? File.ReadAllText(t.file) : string.Empty))
            .Where(t => CodeGenHelpers.NormalizeCodeWhitespace(t.code) != CodeGenHelpers.NormalizeCodeWhitespace(t.originalCode))
            .ToList();
        changes.ForEach(t => File.WriteAllText(t.file, t.code));
        Assert.IsEmpty(changes.Select(t => t.file));

        string GetExtensionMethodName(string interfaceMethodName) =>
            Regex.IsMatch(interfaceMethodName, "^(Try)?Acquire(Async)?$")
                // make it more specific to differentiate when one concrete provider implements multiple provider interfaces
                ? interfaceMethodName.Replace("Async", string.Empty)
                    + interfaceName.Replace("IDistributed", string.Empty)
                    + (interfaceMethodName.EndsWith("Async") ? "Async" : string.Empty)
                : interfaceMethodName;
    }
}
