using Medallion.Threading.Azure;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Azure;

public class AzureTableOptionsBuilderTest
{
    [Test]
    public void TestValidatesDuration()
    {
        var builder = new AzureTableOptionsBuilder();

        // unlike blob leases, the duration is enforced by this library, so there is no 15-60s restriction
        Assert.DoesNotThrow(() => builder.Duration(TimeSpan.FromSeconds(1)));
        Assert.DoesNotThrow(() => builder.Duration(TimeSpan.FromHours(1)));
        Assert.DoesNotThrow(() => builder.Duration(Timeout.InfiniteTimeSpan));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Duration(TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Duration(TimeSpan.FromSeconds(-2)));
    }

    [Test]
    public void TestValidatesRenewalCadence()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AzureTableOptionsBuilder.GetOptions(o => o.RenewalCadence(TimeSpan.FromSeconds(-2))));

        // must be less than the duration, which defaults to 30s
        Assert.Throws<ArgumentOutOfRangeException>(() => AzureTableOptionsBuilder.GetOptions(o => o.RenewalCadence(TimeSpan.FromSeconds(30))));
        Assert.DoesNotThrow(() => AzureTableOptionsBuilder.GetOptions(o => o.RenewalCadence(TimeSpan.FromSeconds(3))));
        Assert.DoesNotThrow(() => AzureTableOptionsBuilder.GetOptions(o => o.RenewalCadence(Timeout.InfiniteTimeSpan)));

        Assert.Throws<ArgumentOutOfRangeException>(() => AzureTableOptionsBuilder.GetOptions(o => o.Duration(TimeSpan.FromSeconds(60)).RenewalCadence(TimeSpan.FromSeconds(60.1))));
        Assert.DoesNotThrow(() => AzureTableOptionsBuilder.GetOptions(o => o.Duration(TimeSpan.FromSeconds(60)).RenewalCadence(TimeSpan.FromSeconds(59.9))));

        // an infinite duration can never be exceeded
        Assert.DoesNotThrow(() => AzureTableOptionsBuilder.GetOptions(o => o.Duration(Timeout.InfiniteTimeSpan).RenewalCadence(TimeSpan.FromDays(1))));
    }

    [Test]
    public void TestValidatesBusyWaitSleepTime()
    {
        var builder = new AzureTableOptionsBuilder();

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.BusyWaitSleepTime(TimeSpan.FromSeconds(-2), TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.BusyWaitSleepTime(Timeout.InfiniteTimeSpan, TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.BusyWaitSleepTime(TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.BusyWaitSleepTime(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1)));

        Assert.DoesNotThrow(() => builder.BusyWaitSleepTime(TimeSpan.Zero, TimeSpan.Zero));
        Assert.DoesNotThrow(() => builder.BusyWaitSleepTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));
    }

    [Test]
    public void TestValidatesNullArguments()
    {
        var builder = new AzureTableOptionsBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.PartitionKey(null!));
        Assert.Throws<ArgumentNullException>(() => builder.RowKey(null!));
        Assert.Throws<ArgumentNullException>(() => builder.AcquireStrategy(null!));
    }

    [Test]
    public void TestDefaults()
    {
        var options = AzureTableOptionsBuilder.GetOptions(null);

        Assert.That(options.Duration.TimeSpan, Is.EqualTo(TimeSpan.FromSeconds(30)));
        Assert.That(options.RenewalCadence.TimeSpan, Is.EqualTo(TimeSpan.FromSeconds(10)));
        Assert.That(options.PartitionKey, Is.Null);
        Assert.That(options.RowKey, Is.Null);
        Assert.That(options.CreateTableIfNotExists, Is.False);
        Assert.That(options.AcquireStrategy, Is.SameAs(AzureTableAcquireStrategySelector.Adaptive));
    }

    [Test]
    public void TestInfiniteDurationImpliesInfiniteRenewalCadence()
    {
        var options = AzureTableOptionsBuilder.GetOptions(o => o.Duration(Timeout.InfiniteTimeSpan));
        Assert.That(options.RenewalCadence.IsInfinite, Is.True);
    }
}
