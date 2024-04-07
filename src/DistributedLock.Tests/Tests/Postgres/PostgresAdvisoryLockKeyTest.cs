using Medallion.Threading.Postgres;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Postgres;

public class PostgresAdvisoryLockKeyTest
{
    [Test]
    public void TestArgumentValidation()
    {
        Assert.Throws<ArgumentNullException>(() => new PostgresAdvisoryLockKey(null!));
        Assert.Throws<FormatException>(() => new PostgresAdvisoryLockKey(new string('A', PostgresAdvisoryLockKey.MaxAsciiLength + 1)));
        Assert.Throws<FormatException>(() => new PostgresAdvisoryLockKey("漢字"));
    }

    [Test]
    public void TestDefault()
    {
        Assert.That(default(PostgresAdvisoryLockKey).ToString(), Is.EqualTo(new string('0', 16)));
        Assert.That(default(PostgresAdvisoryLockKey).HasSingleKey, Is.True);
        Assert.That(default(PostgresAdvisoryLockKey).Key, Is.EqualTo(0));
        AssertEquality(new PostgresAdvisoryLockKey(0), default);
    }

    [Test]
    public void TestAscii()
    {
        var emptyKey = AssertRoundTrips(string.Empty);
        Assert.That(emptyKey.HasSingleKey, Is.False);

        var keys = new HashSet<(int, int)> { emptyKey.Keys };
        for (var i = (char)1; i < 128; ++i)
        {
            for (var j = 1; j <= PostgresAdvisoryLockKey.MaxAsciiLength; ++j)
            {
                var key = AssertRoundTrips(new string(i, j));
                Assert.That(key.HasSingleKey, Is.False);
                Assert.That(keys.Add(key.Keys), Is.True);
            }
        }
    }

    [Test]
    public void TestInt64Construction()
    {
        var key = new PostgresAdvisoryLockKey(1);
        Assert.That(key.HasSingleKey, Is.True);
        Assert.That(key.Key, Is.EqualTo(1L));
        Assert.That(key.ToString(), Is.EqualTo("0000000000000001"));
        AssertEquality(key, new PostgresAdvisoryLockKey(key.ToString()));
    }

    [Test]
    public void TestInt32PairConstruction()
    {
        var key = new PostgresAdvisoryLockKey(3, -1);
        Assert.That(key.HasSingleKey, Is.False);
        Assert.That(key.Keys, Is.EqualTo((3, -1)));
        Assert.That(key.ToString(), Is.EqualTo("00000003,ffffffff"));
        AssertEquality(key, new PostgresAdvisoryLockKey(key.ToString()));
    }

    [Test]
    public void TestNameHashing()
    {
        var key = new PostgresAdvisoryLockKey(new string('漢', 2 * PostgresAdvisoryLockKey.MaxAsciiLength), allowHashing: true);
        Assert.That(key.HasSingleKey, Is.True);
        Assert.That(key.Key, Is.EqualTo(-5707277204051710361));
        AssertEquality(key, new PostgresAdvisoryLockKey(key.ToString()));
    }

    [Test]
    public void TestEquality()
    {
        AssertEquality(new PostgresAdvisoryLockKey(long.MinValue), new PostgresAdvisoryLockKey(long.MinValue));
        AssertInequality(new PostgresAdvisoryLockKey(long.MinValue), new PostgresAdvisoryLockKey(long.MinValue + 1));

        AssertEquality(new PostgresAdvisoryLockKey(int.MinValue, int.MaxValue), new PostgresAdvisoryLockKey(int.MinValue, int.MaxValue));
        AssertInequality(new PostgresAdvisoryLockKey(int.MinValue, int.MaxValue), new PostgresAdvisoryLockKey(int.MinValue, int.MaxValue - 1));

        AssertEquality(new PostgresAdvisoryLockKey("base38"), new PostgresAdvisoryLockKey("base38"));
        AssertInequality(new PostgresAdvisoryLockKey("base38"), new PostgresAdvisoryLockKey("base37"));

        AssertEquality(new PostgresAdvisoryLockKey("ASCII"), new PostgresAdvisoryLockKey("ASCII"));
        AssertInequality(new PostgresAdvisoryLockKey("ASCII"), new PostgresAdvisoryLockKey("ASCIi"));

        AssertInequality(new PostgresAdvisoryLockKey(string.Empty), new PostgresAdvisoryLockKey("\0"));
        AssertInequality(new PostgresAdvisoryLockKey("\0"), new PostgresAdvisoryLockKey("a"));

        AssertEquality(new PostgresAdvisoryLockKey("some very long name", allowHashing: true), new PostgresAdvisoryLockKey("some very long name", allowHashing: true));
        AssertInequality(new PostgresAdvisoryLockKey("some very long name", allowHashing: true), new PostgresAdvisoryLockKey("same very long name", allowHashing: true));

        var names = new[] { "base38", "base37", "ASCII", "ASCIi", "some very long name", "same very long name" };
        foreach (var name1 in names)
        foreach (var name2 in names.Where(n => n != name1))
        {
            AssertInequality(new PostgresAdvisoryLockKey(name1, allowHashing: true), new PostgresAdvisoryLockKey(name2, allowHashing: true));
        }

        AssertEquality(new PostgresAdvisoryLockKey(new string('0', 16)), new PostgresAdvisoryLockKey(0));
        AssertEquality(new PostgresAdvisoryLockKey("00000000,00000000"), new PostgresAdvisoryLockKey(0, 0));
        AssertEquality(new PostgresAdvisoryLockKey(new string('\0', PostgresAdvisoryLockKey.MaxAsciiLength)), new PostgresAdvisoryLockKey(0, 0));
        AssertInequality(new PostgresAdvisoryLockKey(0), new PostgresAdvisoryLockKey(0, 0));
    }

    private static void AssertInequality(PostgresAdvisoryLockKey a, PostgresAdvisoryLockKey b)
    {
        Assert.That(b, Is.Not.EqualTo(a));
        Assert.That(a == b, Is.False);
        Assert.That(a != b, Is.True);
        Assert.That(b.GetHashCode(), Is.Not.EqualTo(a.GetHashCode()));
        if (a.HasSingleKey && b.HasSingleKey)
        {
            Assert.That(b.Key, Is.Not.EqualTo(a.Key));
        }
        else if (!a.HasSingleKey && !b.HasSingleKey)
        {
            Assert.That(b.Keys, Is.Not.EqualTo(a.Keys));
        }
    }

    private static void AssertEquality(PostgresAdvisoryLockKey a, PostgresAdvisoryLockKey b)
    {
        Assert.That(b, Is.EqualTo(a));
        Assert.That(a == b, Is.True);
        Assert.That(a != b, Is.False);
        Assert.That(b.GetHashCode(), Is.EqualTo(a.GetHashCode()));
        if (a.HasSingleKey)
        {
            Assert.That(b.Key, Is.EqualTo(a.Key));
        }
        else
        {
            Assert.That(b.Keys, Is.EqualTo(a.Keys));
        }
    }

    private static PostgresAdvisoryLockKey AssertRoundTrips(string name)
    {
        var key1 = new PostgresAdvisoryLockKey(name);
        var key2 = new PostgresAdvisoryLockKey(key1.ToString());
        var key3 = new PostgresAdvisoryLockKey(name, allowHashing: true);
        AssertEquality(key1, key2);
        AssertEquality(key1, key3);
        Assert.That(key2.ToString(), Is.EqualTo(key1.ToString()));
        Assert.That(key3.ToString(), Is.EqualTo(key1.ToString()));
        return key1;
    }
}
