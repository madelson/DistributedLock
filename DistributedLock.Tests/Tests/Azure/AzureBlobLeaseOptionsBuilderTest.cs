using Medallion.Threading.Azure;
using Medallion.Threading.Internal;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Medallion.Threading.Tests.Azure;

public class AzureBlobLeaseOptionsBuilderTest
{
    [Test]
    public void TestValidatesDuration()
    {
        var builder = new AzureBlobLeaseOptionsBuilder();

        Assert.DoesNotThrow(() => builder.Duration(TimeSpan.FromSeconds(15)));
        Assert.DoesNotThrow(() => builder.Duration(TimeSpan.FromSeconds(60)));
        Assert.DoesNotThrow(() => builder.Duration(Timeout.InfiniteTimeSpan));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Duration(TimeSpan.FromSeconds(14)));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Duration(TimeSpan.FromSeconds(61)));
    }

    [Test]
    public void TestValidatesRenewalCadence()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AzureBlobLeaseOptionsBuilder.GetOptions(o => o.RenewalCadence(TimeSpan.FromSeconds(-1))));
        Assert.DoesNotThrow(() => AzureBlobLeaseOptionsBuilder.GetOptions(o => o.RenewalCadence(TimeSpan.Zero)));

        Assert.Throws<ArgumentOutOfRangeException>(() => AzureBlobLeaseOptionsBuilder.GetOptions(o => o.RenewalCadence(TimeSpan.FromSeconds(30))));
        Assert.DoesNotThrow(() => AzureBlobLeaseOptionsBuilder.GetOptions(o => o.RenewalCadence(TimeSpan.FromSeconds(3))));
        Assert.DoesNotThrow(() => AzureBlobLeaseOptionsBuilder.GetOptions(o => o.RenewalCadence(Timeout.InfiniteTimeSpan)));

        Assert.Throws<ArgumentOutOfRangeException>(() => AzureBlobLeaseOptionsBuilder.GetOptions(o => o.Duration(TimeSpan.FromSeconds(60)).RenewalCadence(TimeSpan.FromSeconds(60.1))));
        Assert.DoesNotThrow(() => AzureBlobLeaseOptionsBuilder.GetOptions(o => o.Duration(TimeSpan.FromSeconds(60)).RenewalCadence(TimeSpan.FromSeconds(59.9))));

        Assert.DoesNotThrow(() => AzureBlobLeaseOptionsBuilder.GetOptions(o => o.Duration(Timeout.InfiniteTimeSpan).RenewalCadence(Timeout.InfiniteTimeSpan)));
    }

    [Test]
    public void TestValidatesBusyWaitSleepTime()
    {
        var builder = new AzureBlobLeaseOptionsBuilder();

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.BusyWaitSleepTime(Timeout.InfiniteTimeSpan, TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.BusyWaitSleepTime(TimeSpan.FromSeconds(-1), TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.BusyWaitSleepTime(TimeSpan.MaxValue, TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.BusyWaitSleepTime(TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.BusyWaitSleepTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.BusyWaitSleepTime(TimeSpan.FromSeconds(1), TimeSpan.MaxValue));

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.BusyWaitSleepTime(TimeSpan.FromSeconds(1.1), TimeSpan.FromSeconds(1)));

        Assert.DoesNotThrow(() => builder.BusyWaitSleepTime(TimeSpan.Zero, TimeSpan.Zero));
        Assert.DoesNotThrow(() => builder.BusyWaitSleepTime(TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(4)));
    }

    [Test]
    public void TestDisablesAutoRenewalIfDurationIsInfinite()
    {
        var options = AzureBlobLeaseOptionsBuilder.GetOptions(b => b.Duration(Timeout.InfiniteTimeSpan));
        Assert.IsTrue(options.renewalCadence.IsInfinite);
    }
}
