using Medallion.Threading.Internal;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Core;

[Category("CI")]
public class TimeoutValueTest
{
    [Test]
    public void TestArgumentValidation()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimeoutValue(TimeSpan.FromMilliseconds(-2)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimeoutValue(TimeSpan.FromMilliseconds((long)int.MaxValue + 1)));
    }

    [Test]
    public void TestProperties()
    {
        Assert.That(default(TimeoutValue).IsZero, Is.True);
        Assert.That(default(TimeoutValue).IsInfinite, Is.False);
        Assert.That(default(TimeoutValue).InMilliseconds, Is.EqualTo(0));
        Assert.That(default(TimeoutValue).InSeconds, Is.EqualTo(0));

        TimeoutValue infinite = Timeout.InfiniteTimeSpan;
        Assert.That(infinite.IsZero, Is.False);
        Assert.That(infinite.IsInfinite, Is.True);
        Assert.That(infinite.InMilliseconds, Is.EqualTo(-1));
        Assert.Throws<InvalidOperationException>(() => infinite.InSeconds.ToString());

        TimeoutValue normal = TimeSpan.FromSeconds(10.4);
        Assert.That(normal.IsZero, Is.False);
        Assert.That(normal.IsInfinite, Is.False);
        Assert.That(normal.InMilliseconds, Is.EqualTo(10400));
        Assert.That(normal.InSeconds, Is.EqualTo(10));
    }

    [Test]
    public void TestConversion()
    {
        Assert.That(new TimeoutValue(Timeout.InfiniteTimeSpan), Is.EqualTo((TimeoutValue)default(TimeSpan?)));

        CheckEquality(Timeout.InfiniteTimeSpan);
        CheckEquality(TimeSpan.FromSeconds(101.3));
        CheckEquality(TimeSpan.FromTicks(1));
        CheckEquality(TimeSpan.Zero);

        static void CheckEquality(TimeSpan value) => Assert.That(((TimeoutValue)value).InMilliseconds, Is.EqualTo((int)value.TotalMilliseconds));
    }

    [Test]
    public void TestEquality()
    {
        var timeSpans = new double[] { Timeout.Infinite, 0, 1, 1000, 10101 }.Select(TimeSpan.FromMilliseconds)
            .ToArray();

        foreach (var a in timeSpans)
        {
            foreach (var b in timeSpans)
            {
                TimeoutValue aValue = a, bValue = b;

                if (a == b)
                {
                    Assert.That(aValue == bValue, Is.True);
                    Assert.That(aValue != bValue, Is.False);
                    Assert.That(aValue.Equals(bValue), Is.True);
                    Assert.That(aValue.Equals((object)bValue), Is.True);
                    Assert.That(Equals(aValue, bValue), Is.True);
                    Assert.That(bValue.GetHashCode(), Is.EqualTo(aValue.GetHashCode()));
                }
                else
                {
                    Assert.That(aValue == bValue, Is.False);
                    Assert.That(aValue != bValue, Is.True);
                    Assert.That(aValue.Equals(bValue), Is.False);
                    Assert.That(aValue.Equals((object)bValue), Is.False);
                    Assert.That(Equals(aValue, bValue), Is.False);
                    Assert.That(bValue.GetHashCode(), Is.Not.EqualTo(aValue.GetHashCode()));
                }
            }
        }
    }

    [Test]
    public void TestComparison()
    {
        new TimeoutValue(Timeout.InfiniteTimeSpan).CompareTo(Timeout.InfiniteTimeSpan).ShouldEqual(0);
        new TimeoutValue(TimeSpan.FromSeconds(1)).CompareTo(TimeSpan.FromSeconds(1)).ShouldEqual(0);

        new TimeoutValue(Timeout.InfiniteTimeSpan).CompareTo(TimeSpan.FromMilliseconds(int.MaxValue)).ShouldEqual(1);
        new TimeoutValue(TimeSpan.FromMilliseconds(int.MaxValue)).CompareTo(Timeout.InfiniteTimeSpan).ShouldEqual(-1);

        new TimeoutValue(TimeSpan.Zero).CompareTo(TimeSpan.FromSeconds(1)).ShouldEqual(-1);
        new TimeoutValue(TimeSpan.FromSeconds(1)).CompareTo(TimeSpan.Zero).ShouldEqual(1);
    }
}
