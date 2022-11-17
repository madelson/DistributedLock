using Medallion.Threading.ZooKeeper;
using NUnit.Framework;

namespace Medallion.Threading.Tests.ZooKeeper;

[Category("CI")]
public class ZooKeeperSequentialPathHelperTest
{
    [TestCase("/", "a", ExpectedResult = null)]
    [TestCase("/ba0000000002", "a", ExpectedResult = null)]
    [TestCase("/ba0000000002", "ba", ExpectedResult = 2)]
    [TestCase("/ba000000002", "ba", ExpectedResult = null)]
    [TestCase("/ba00000000112", "ba", ExpectedResult = null)]
    [TestCase("/ba-000000002", "ba", ExpectedResult = -2)]
    [TestCase("/ba0000000002", "/ba", ExpectedResult = null)]
    [TestCase("/c/d/ba0000000402", "ba", ExpectedResult = 402)]
    [TestCase("lock-2147483647", "lock-", ExpectedResult = int.MaxValue)]
    [TestCase("read--000000001", "read-", ExpectedResult = -1)]
    [TestCase("write--2147483648", "write-", ExpectedResult = int.MinValue)]
    [TestCase("x 000000002", "x", ExpectedResult = null)]
    [TestCase("x000000002 ", "x", ExpectedResult = null)]
    public int? TestGetSequenceNumberOrDefault(string pathOrName, string prefix) =>
        ZooKeeperSequentialPathHelper.GetSequenceNumberOrDefault(pathOrName, prefix);

    [Test]
    public Task TestFilterAndSortLowPositiveNumbers() => TestFilterAndSortHelper(
        new[] { 5, 3, 1 },
        expectedSequenceNumbers: new[] { 1, 3, 5 }
    );

    [Test]
    public Task TestFilterAndSortMediumNegativeNumbers() => TestFilterAndSortHelper(
        new[] { -1000000000, -1000000050, -1000000500 },
        expectedSequenceNumbers: new[] { -1000000500, -1000000050, -1000000000 }
    );

    [Test]
    public Task TestFilterAndSortHighPositiveAndLowNegativeNumbers() => TestFilterAndSortHelper(
        new[] { int.MaxValue, int.MaxValue - 1, int.MaxValue - 100, int.MinValue, int.MinValue + 9, int.MinValue + 90 },
        expectedSequenceNumbers: new[] { int.MaxValue - 100, int.MaxValue - 1, int.MaxValue, int.MinValue, int.MinValue + 9, int.MinValue + 90 }
    );

    [Test]
    public Task TestFilterAndSortHighNegativeAndLowPositiveNumbers() => TestFilterAndSortHelper(
        new[] { 1, 15, 6, -1, -3 },
        expectedSequenceNumbers: new[] { -3, -1, 1, 6, 15 }
    );

    [Test]
    public Task TestFilterAndSortLowAndHighPositiveNumbersLowsOlder() => TestFilterAndSortHelper(
        new[] { 4, 2, 0, int.MaxValue, int.MaxValue - 3, int.MaxValue - 5 },
        expectedSequenceNumbers: new[] { 0, 2, 4, int.MaxValue - 5, int.MaxValue - 3, int.MaxValue },
        creationTimes: new Dictionary<int, long>
        {
            [0] = 1,
            [2] = 1,
            [4] = 2,
            [int.MaxValue - 5] = 3,
            [int.MaxValue - 3] = 4,
            [int.MaxValue] = 5,
        }
    );

    [Test]
    public Task TestFilterAndSortLowAndHighPositiveNumbersHighsOlder() => TestFilterAndSortHelper(
        new[] { 4, 2, 0, int.MaxValue, int.MaxValue - 3, int.MaxValue - 5 },
        expectedSequenceNumbers: new[] { int.MaxValue - 5, int.MaxValue - 3, int.MaxValue, 0, 2, 4 },
        creationTimes: new Dictionary<int, long>
        {
            [0] = 3,
            [2] = 4,
            [4] = 5,
            [int.MaxValue - 5] = 1,
            [int.MaxValue - 3] = 1,
            [int.MaxValue] = 2,
        }
    );

    [Test]
    public Task TestFilterAndSortLowAndHighNegativeNumbersLowsOlder() => TestFilterAndSortHelper(
        new[] { -300, -301, -302, int.MinValue, int.MinValue + 10, int.MinValue + 100 },
        expectedSequenceNumbers: new[] { int.MinValue, int.MinValue + 10, int.MinValue + 100, -302, -301, -300 },
        creationTimes: new Dictionary<int, long>
        {
            [-302] = 300,
            [-301] = 300,
            [-300] = 300,
            [int.MinValue] = 100,
            [int.MinValue + 10] = 100,
            [int.MinValue + 100] = 100,
        }
    );

    [Test]
    public Task TestFilterAndSortLowAndHighNegativeNumbersHighsOlder() => TestFilterAndSortHelper(
        new[] { -300, -301, -302, int.MinValue, int.MinValue + 10, int.MinValue + 100 },
        expectedSequenceNumbers: new[] { -302, -301, -300, int.MinValue, int.MinValue + 10, int.MinValue + 100 },
        creationTimes: new Dictionary<int, long>
        {
            [-302] = 30,
            [-301] = 30,
            [-300] = 30,
            [int.MinValue] = 100,
            [int.MinValue + 10] = 100,
            [int.MinValue + 100] = 100,
        }
    );

    [Test]
    public Task TestFilterAndSortHighNegativeAndPositiveNumbersPositivesOlder() => TestFilterAndSortHelper(
        new[] { int.MaxValue, -1000 },
        expectedSequenceNumbers: new[] { int.MaxValue, -1000 },
        creationTimes: new Dictionary<int, long>
        {
            [int.MaxValue] = 1,
            [-1000] = 2000,
        }
    );

    [Test]
    public Task TestFilterAndSortHighNegativeAndPositiveNumbersNegativesOlder() => TestFilterAndSortHelper(
        new[] { int.MaxValue, -1000 },
        expectedSequenceNumbers: new[] { -1000, int.MaxValue },
        creationTimes: new Dictionary<int, long>
        {
            [int.MaxValue] = long.MaxValue,
            [-1000] = long.MaxValue - 1,
        }
    );

    [Test]
    public Task TestFilterAndSortLowNegativeAndPositiveNumbersPositivesOlder() => TestFilterAndSortHelper(
        new[] { int.MinValue, 2_000_000 },
        expectedSequenceNumbers: new[] { 2_000_000, int.MinValue },
        creationTimes: new Dictionary<int, long>
        {
            [int.MinValue] = 10,
            [2_000_000] = 1,
        }
    );

    [Test]
    public Task TestFilterAndSortLowNegativeAndPositiveNumbersNegativesOlder() => TestFilterAndSortHelper(
        new[] { int.MinValue, 2_000_000 },
        expectedSequenceNumbers: new[] { int.MinValue, 2_000_000 },
        creationTimes: new Dictionary<int, long>
        {
            [int.MinValue] = 1,
            [2_000_000] = 10,
        }
    );

    [Test]
    public Task TestFilterAndSortEmpty() => TestFilterAndSortHelper(Array.Empty<int>(), expectedSequenceNumbers: Array.Empty<int>());

    [Test]
    public Task TestFilterAndSortEmptyAfterChecking() => TestFilterAndSortHelper(
        Enumerable.Range(1, 100).Concat(new[] { int.MaxValue }).ToArray(),
        expectedSequenceNumbers: Array.Empty<int>(),
        creationTimes: new Dictionary<int, long> { [-1] = 1 }
    );

    [Test]
    public Task TestFilterAndSortFilteredAfterChecking() => TestFilterAndSortHelper(
        new[] { 1, 2, 3, int.MaxValue - 10, int.MaxValue },
        expectedSequenceNumbers: new[] { int.MaxValue - 10, 2 },
        creationTimes: new Dictionary<int, long>
        {
            [int.MaxValue - 10] = long.MinValue,
            [2] = long.MaxValue
        }
    );

    private static async Task TestFilterAndSortHelper(
        IReadOnlyList<int> sequenceNumbers, 
        IReadOnlyList<int> expectedSequenceNumbers,
        Dictionary<int, long>? creationTimes = null)
    {
        var random = new Random(12345);
        var paths = sequenceNumbers.Select(n => MakeName(random.Next(2) == 0 ? "a" : "b", n))
            .Concat(new[] { MakeName("c", 0), MakeName("d", 1), "a", "b" })
            .OrderBy(_ => random.Next())
            .ToArray();
        var parentNode = $"/parent{random.Next()}";

        var result = await ZooKeeperSequentialPathHelper.FilterAndSortAsync(
            parentNode,
            paths,
            n => Task.FromResult(
                (creationTimes ?? throw new AssertionException("Shouldn't check creation time")).TryGetValue(GetSequenceNumber(n), out var creationTime)
                    ? creationTime
                    : default(long?)
            ),
            prefix: "a",
            alternatePrefix: "b"
        );

        CollectionAssert.AreEqual(expectedSequenceNumbers, result.Select(t => t.SequenceNumber));
        foreach (var info in result)
        {
            info.Path.ShouldEqual($"{parentNode}/{MakeName(info.Prefix, info.SequenceNumber)}");
        }

        int GetSequenceNumber(string name) => ZooKeeperSequentialPathHelper.GetSequenceNumberOrDefault(name, "a")
            ?? ZooKeeperSequentialPathHelper.GetSequenceNumberOrDefault(name, "b")
            ?? throw new AssertionException($"Can't get sequence number for '{name}'");
    }

    private static string MakeName(string prefix, int sequenceNumber) => $"{prefix}{sequenceNumber:0000000000}";

    private static Task<long?> CannotGetNodeCreationTime(string node) => throw new AssertionException("Should not be called");
}
