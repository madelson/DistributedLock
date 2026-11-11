using Medallion.Threading.Azure;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Azure;

public class AzureTableAcquireStrategySelectorTest
{
    [Test]
    public void TestValidatesNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => AzureTableAcquireStrategySelector.FromFunc(null!));
    }

    [Test]
    public void TestValidatesConstantStrategy()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AzureTableAcquireStrategySelector.FromConstant((AzureTableAcquireStrategy)(-1)));
        Assert.DoesNotThrow(() => AzureTableAcquireStrategySelector.FromConstant(AzureTableAcquireStrategy.ReadFirst));
    }

    [Test]
    public void TestAdaptiveUsesReadFirstOnlyWhenRowIsKnownToExist()
    {
        var selector = AzureTableAcquireStrategySelector.Adaptive;

        // no information yet: assume the uncontended case
        Assert.That(Select(selector, rowExists: null), Is.EqualTo(AzureTableAcquireStrategy.InsertFirst));
        Assert.That(Select(selector, rowExists: false), Is.EqualTo(AzureTableAcquireStrategy.InsertFirst));
        // contention observed: reading avoids a guaranteed 409
        Assert.That(Select(selector, rowExists: true), Is.EqualTo(AzureTableAcquireStrategy.ReadFirst));
    }

    [Test]
    public void TestFuncReceivesContext()
    {
        AzureTableAcquireContext observed = default;
        var selector = AzureTableAcquireStrategySelector.FromFunc(context =>
        {
            observed = context;
            return AzureTableAcquireStrategy.ReadFirst;
        });

        var context = new AzureTableAcquireContext("name", "pk", "rk", attemptNumber: 3, rowExists: true);
        Assert.That(Select(selector, context), Is.EqualTo(AzureTableAcquireStrategy.ReadFirst));
        Assert.That(observed, Is.EqualTo(context));
    }

    [Test]
    public void TestRejectsUndefinedStrategyFromFunc()
    {
        var selector = AzureTableAcquireStrategySelector.FromFunc(_ => (AzureTableAcquireStrategy)42);
        Assert.Throws<InvalidOperationException>(() => Select(selector, rowExists: null));
    }

    [Test]
    public void TestImplicitConversions()
    {
        AzureTableAcquireStrategySelector fromConstant = AzureTableAcquireStrategy.ReadFirst;
        Assert.That(Select(fromConstant, rowExists: null), Is.EqualTo(AzureTableAcquireStrategy.ReadFirst));

        AzureTableAcquireStrategySelector fromFunc =
            (Func<AzureTableAcquireContext, AzureTableAcquireStrategy>)(_ => AzureTableAcquireStrategy.InsertFirst);
        Assert.That(Select(fromFunc, rowExists: true), Is.EqualTo(AzureTableAcquireStrategy.InsertFirst));
    }

    private static AzureTableAcquireStrategy Select(AzureTableAcquireStrategySelector selector, bool? rowExists) =>
        Select(selector, new AzureTableAcquireContext("name", "pk", "rk", attemptNumber: 0, rowExists: rowExists));

    private static AzureTableAcquireStrategy Select(AzureTableAcquireStrategySelector selector, AzureTableAcquireContext context) =>
        selector.Select(context);
}
