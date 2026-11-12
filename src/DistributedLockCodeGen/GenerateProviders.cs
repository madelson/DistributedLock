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
    public static readonly IReadOnlyList<string> Interfaces =
    [
        "IDistributedLock",
        "IDistributedReaderWriterLock",
        "IDistributedUpgradeableReaderWriterLock",
        "IDistributedSemaphore"
    ];

    private static readonly IReadOnlyDictionary<string, string> ExcludedInterfacesForCompositeMethods = new Dictionary<string, string>
    {
        ["IDistributedUpgradeableReaderWriterLock"] = "a composite acquire operation must be able to roll back and upgrade does not support that."
    };

    [TestCaseSource(nameof(Interfaces))]
    public void GenerateProviderInterfaceAndExtensions(string interfaceName)
    {
        var interfaceFile = Directory
            .GetFiles(CodeGenHelpers.SolutionDirectory, interfaceName + ".cs", SearchOption.AllDirectories)
            .Single();
        var providerInterfaceName = interfaceName + "Provider";

        var createMethodName = $"Create{interfaceName.Replace("IDistributed", string.Empty)}";
        var providerInterfaceCode = $$"""
            // AUTO-GENERATED
            namespace Medallion.Threading;

            /// <summary>
            /// Acts as a factory for <see cref="{{interfaceName}}"/> instances of a certain type. This interface may be
            /// easier to use than <see cref="{{interfaceName}}"/> in dependency injection scenarios.
            /// </summary>
            public interface {{providerInterfaceName}}{{(interfaceName == "IDistributedUpgradeableReaderWriterLock" ? ": IDistributedReaderWriterLockProvider" : string.Empty)}}
            {
                /// <summary>
                /// Constructs an <see cref="{{interfaceName}}"/> instance with the given <paramref name="name"/>.
                /// </summary>
                {{interfaceName}} {{createMethodName}}(string name{{(interfaceName.Contains("Semaphore") ? ", int maxCount" : string.Empty)}});
            }
            """;

        var interfaceMethods = Regex.Matches(
            File.ReadAllText(interfaceFile),
            @"(?<returnType>\S+) (?<name>\S+)\((?<parameters>((?<parameterType>\S*) (?<parameterName>\w+)[^,)]*(\, )?)*)\);",
            RegexOptions.ExplicitCapture
        );

        var extensionSingleMethodBodies = interfaceMethods
            .Select(m =>
                $"""
                     /// <summary>
                     /// Equivalent to calling <see cref="{providerInterfaceName}.{createMethodName}(string{(interfaceName.Contains("Semaphore") ? ", int" : string.Empty)})" /> and then
                     /// <see cref="{interfaceName}.{m.Groups["name"].Value}({string.Join(", ", m.Groups["parameterType"].Captures.Select(c => c.Value))})" />.
                     /// </summary>
                     public static {m.Groups["returnType"].Value} {GetExtensionMethodName(m.Groups["name"].Value)}(this {providerInterfaceName} provider, string name{(interfaceName.Contains("Semaphore") ? ", int maxCount" : string.Empty)}, {m.Groups["parameters"].Value}) =>
                         (provider ?? throw new ArgumentNullException(nameof(provider))).{createMethodName}(name{(interfaceName.Contains("Semaphore") ? ", maxCount" : string.Empty)}).{m.Groups["name"].Value}({string.Join(", ", m.Groups["parameterName"].Captures.Select(c => c.Value))});
                 """
            );

        var extensionCompositeMethodBodies = ExcludedInterfacesForCompositeMethods.TryGetValue(interfaceName, out var exclusionReason)
            ?
            [
                $"""
                     // Composite methods are not supported for {interfaceName}
                     // because {exclusionReason}
                 """
            ]
            : interfaceMethods
                .Select(m =>
                    {
                        var baseExtensionMethodName = GetExtensionMethodName(m.Groups["name"].Value);
                        var isAsync = baseExtensionMethodName.EndsWith("Async");
                        var isTry = baseExtensionMethodName.StartsWith("Try");
                        var extensionMethodName = baseExtensionMethodName.Replace("Async", "")
                            .Replace("Acquire", "AcquireAll")
                            + "s"
                            + (isAsync ? "Async" : "");
                        var isSemaphore = interfaceName.Contains("Semaphore");
                        var isReaderWriterLock = interfaceName.Contains("ReaderWriterLock");
                        var primitiveNoun = isSemaphore ? "semaphore" : isReaderWriterLock ? "reader-writer lock" : "lock";
                        var acquiredNoun = isReaderWriterLock ? (m.Groups["name"].Value.Contains("Read") ? "read locks" : "write locks") : primitiveNoun + "s";
                        string MaxCountArg(string prefix = "") => isSemaphore ? prefix + "maxCount, " : "";
                        var internalMethodName = $"Try{extensionMethodName.Replace("Try", "").Replace("Async", "InternalAsync")}";
                        var getHandleMethodName = $"GetHandleOr{(isTry ? "Default" : "Timeout")}";
                        var timeoutType = m.Groups["parameterType"].Captures[0].Value;
                        var summary = $"""
                                /// <summary>
                                /// Equivalent to calling <see cref="{providerInterfaceName}.{createMethodName}(string{(isSemaphore ? ", int" : string.Empty)})" /> for each name in <paramref name="names" /> and then
                                /// <see cref="{interfaceName}.{m.Groups["name"].Value}({string.Join(", ", m.Groups["parameterType"].Captures.Select(c => c.Value))})" /> on each created instance, combining the results into a composite handle.
                                /// </summary>
                            """;
                        var leadingParamDocs = $"""
                                /// <param name="provider">The provider used to create each {primitiveNoun}.</param>
                                /// <param name="names">{(isReaderWriterLock ? $"The names of the reader-writer locks on which to acquire {acquiredNoun}." : $"The names of the {acquiredNoun} to acquire.")}</param>{(isSemaphore ? """

                                /// <param name="maxCount">The max count of each semaphore.</param>
                            """ : string.Empty)}
                                /// <param name="timeout">The total time to wait for all {acquiredNoun} to be acquired.</param>
                            """;
                        var cancellationTokenParamDoc = """
                                /// <param name="cancellationToken">Specifies a token by which the wait can be canceled.</param>
                            """;

                        return $"""
                            {summary}
                            {leadingParamDocs}
                            {cancellationTokenParamDoc}
                                public static {m.Groups["returnType"].Value} {extensionMethodName}(this {providerInterfaceName} provider, IReadOnlyList<string> names{(isSemaphore ? ", int maxCount" : string.Empty)}, {m.Groups["parameters"].Value}) =>
                                    {(
                                        isAsync
                                            ? $"provider.{internalMethodName}(names, {MaxCountArg()}timeout, observeLockLostToken: false, cancellationToken).{getHandleMethodName}();"
                                            : $"SyncViaAsync.Run(static s => s.provider.{extensionMethodName}Async(s.names, {MaxCountArg("s.")}s.timeout, s.cancellationToken), (provider, names, {MaxCountArg()}timeout, cancellationToken));"
                                    )}

                            {summary}
                            {leadingParamDocs}
                                /// <param name="observeLockLostToken">
                                /// Whether to observe the <see cref="IDistributedSynchronizationHandle.HandleLostToken"/> of already-acquired handles while acquiring the remaining ones.
                                /// If a handle is lost mid-acquisition, all acquired handles are released and {(isTry ? "null is returned" : "a <see cref=\"DistributedLockLostException\"/> is thrown")}.
                                /// Has no effect when only one name is given. Note that accessing the token may start background monitoring for some providers.
                                /// </param>
                            {cancellationTokenParamDoc}
                                public static {m.Groups["returnType"].Value} {extensionMethodName}(this {providerInterfaceName} provider, IReadOnlyList<string> names{(isSemaphore ? ", int maxCount" : string.Empty)}, {timeoutType} timeout, bool observeLockLostToken, CancellationToken cancellationToken) =>
                                    {(
                                        isAsync
                                            ? $"provider.{internalMethodName}(names, {MaxCountArg()}timeout, observeLockLostToken, cancellationToken).{getHandleMethodName}();"
                                            : $"SyncViaAsync.Run(static s => s.provider.{extensionMethodName}Async(s.names, {MaxCountArg("s.")}s.timeout, s.observeLockLostToken, s.cancellationToken), (provider, names, {MaxCountArg()}timeout, observeLockLostToken, cancellationToken));"
                                    )}
                            """;
                    }
                );

        var providerExtensionsName = providerInterfaceName.TrimStart('I') + "Extensions";

        var providerExtensionsCode = $$"""
            // AUTO-GENERATED

            using Medallion.Threading.Internal;

            namespace Medallion.Threading;

            /// <summary>
            /// Productivity helper methods for <see cref="{{providerInterfaceName}}" />
            /// </summary>
            public static class {{providerExtensionsName}}
            {
                # region Single Lock Methods

            {{string.Join(Environment.NewLine + Environment.NewLine, extensionSingleMethodBodies)}}

                # endregion

                # region Composite Lock Methods

            {{string.Join(Environment.NewLine + Environment.NewLine, extensionCompositeMethodBodies)}}

                # endregion
            }
            """;

        var changes = new[]
            {
                (name: providerInterfaceName, code: providerInterfaceCode),
                (name: providerExtensionsName, code: providerExtensionsCode)
            }
            .Select(t => (file: Path.Combine(Path.GetDirectoryName(interfaceFile)!, t.name + ".cs"), t.code))
            .Select(t => (t.file, t.code, originalCode: File.Exists(t.file) ? File.ReadAllText(t.file) : string.Empty))
            .Where(t => CodeGenHelpers.NormalizeCodeWhitespace(t.code) !=
                        CodeGenHelpers.NormalizeCodeWhitespace(t.originalCode))
            .ToList();
        changes.ForEach(t => File.WriteAllText(t.file, t.code));
        Assert.That(changes.Select(t => t.file), Is.Empty);

        string GetExtensionMethodName(string interfaceMethodName) =>
            Regex.IsMatch(interfaceMethodName, "^(Try)?Acquire(Async)?$")
                // make it more specific to differentiate when one concrete provider implements multiple provider interfaces
                ? interfaceMethodName.Replace("Async", string.Empty)
                  + interfaceName.Replace("IDistributed", string.Empty)
                  + (interfaceMethodName.EndsWith("Async") ? "Async" : string.Empty)
                : interfaceMethodName;
    }
}
