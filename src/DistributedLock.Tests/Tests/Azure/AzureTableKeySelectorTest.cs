using Medallion.Threading.Azure;
using NUnit.Framework;
using TableClient = Azure.Data.Tables.TableClient;

namespace Medallion.Threading.Tests.Azure;

public class AzureTableKeySelectorTest
{
    [Test]
    public void TestValidatesNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => AzureTableKeySelector.FromTemplate(null!));
        Assert.Throws<ArgumentNullException>(() => AzureTableKeySelector.FromFunc(null!));
    }

    [Test]
    public void TestTemplateWithPlaceholderSubstitutesName()
    {
        AzureTableKeySelector selector = "lock-" + AzureTableKeySelector.NamePlaceholder;
        Assert.That(Select(selector, "abc"), Is.EqualTo("lock-abc"));
    }

    [Test]
    public void TestTemplateWithoutPlaceholderIsConstant()
    {
        AzureTableKeySelector selector = "constant";
        Assert.That(Select(selector, "abc"), Is.EqualTo("constant"));
        Assert.That(Select(selector, "def"), Is.EqualTo("constant"));
    }

    [Test]
    public void TestFuncConversion()
    {
        AzureTableKeySelector selector = (Func<string, string>)(name => name.ToUpperInvariant());
        Assert.That(Select(selector, "abc"), Is.EqualTo("ABC"));
    }

    [Test]
    public void TestRejectsInvalidKeys()
    {
        // a custom selector's output is validated rather than silently sanitized
        foreach (var invalid in new[] { "a/b", "a\\b", "a#b", "a?b", "a\tb" })
        {
            var selector = AzureTableKeySelector.FromFunc(_ => invalid);
            Assert.Throws<ArgumentException>(() => Select(selector, "name"), $"should reject '{invalid}'");
        }
    }

    [Test]
    public void TestRejectsNullEmptyAndOverlongKeys()
    {
        Assert.Throws<ArgumentException>(() => Select(AzureTableKeySelector.FromFunc(_ => null!), "name"));
        Assert.Throws<ArgumentException>(() => Select(AzureTableKeySelector.FromFunc(_ => string.Empty), "name"));
        Assert.Throws<ArgumentException>(() => Select(AzureTableKeySelector.FromFunc(_ => new string('a', 1025)), "name"));
        Assert.DoesNotThrow(() => Select(AzureTableKeySelector.FromFunc(_ => new string('a', 1024)), "name"));
    }

    /// <summary>
    /// Exercises the internal validation path through the public constructor, which is the only way
    /// selectors are ever applied.
    /// </summary>
    private static string Select(AzureTableKeySelector selector, string name) =>
        new AzureTableDistributedLock(
            new TableClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultTableName),
            name,
            o => o.RowKey(selector)
        ).RowKey;
}
