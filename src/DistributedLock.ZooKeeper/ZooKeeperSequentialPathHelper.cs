using Medallion.Threading.Internal;
using System.Globalization;

namespace Medallion.Threading.ZooKeeper;

internal static class ZooKeeperSequentialPathHelper
{
    /// <summary>
    /// Given a set of child node names (<paramref name="childrenNames"/>), filters them to include only sequential nodes
    /// with the prefix <paramref name="prefix"/> or <paramref name="alternatePrefix"/>.
    /// 
    /// Then, sorts the nodes from oldest to newest. In most, cases, this sort can be done purely using the sequence number. However, because sequence
    /// numbers roll over at <see cref="int.MaxValue"/>, more complex logic is needed to get a correct sort in certain scenarios.
    /// </summary>
    public static async ValueTask<(string Path, int SequenceNumber, string Prefix)[]> FilterAndSortAsync(
        string parentNode,
        IEnumerable<string> childrenNames,
        Func<string, Task<long?>> getNodeCreationTimeAsync,
        string prefix,
        string? alternatePrefix = null)
    {
        var ephemeralChildrenWithPrefix = GetEphemeralChildrenWithPrefix();

        if (ephemeralChildrenWithPrefix.Count == 0) { return Array.Empty<(string, int, string)>(); }

        // first, sort by the unsigned value
        ephemeralChildrenWithPrefix.Sort((a, b) => a.UnsignedSequenceNumber.CompareTo(b.UnsignedSequenceNumber));

        // next, measure the gaps between each pair of sequential values to see if the break point is obvious

        // This is > 90% of the uint space; there can only be one gap of this size and if there is such a gap
        // then we can safely assume that the gap identifies the end of the sequence 
        const uint LargeGapSize = 4_000_000_000u;

        var maxGap = 0u;
        var maxGapEndIndex = -1;
        for (var i = 0; i < ephemeralChildrenWithPrefix.Count; ++i)
        {
            var gapEndIndex = (i + 1) % ephemeralChildrenWithPrefix.Count;
            var gap = unchecked(ephemeralChildrenWithPrefix[gapEndIndex].UnsignedSequenceNumber - ephemeralChildrenWithPrefix[i].UnsignedSequenceNumber);
            if (gap > maxGap)
            {
                maxGap = gap;
                maxGapEndIndex = gapEndIndex;
            }
        }
        if (maxGap >= LargeGapSize)
        {
            return ReorderByLowestIndex(maxGapEndIndex);
        }

        // finally, fall back to determining the start point via creation time
        var creationTimeTasksByChildPath = ephemeralChildrenWithPrefix.ToDictionary(t => t.Path, t => getNodeCreationTimeAsync(t.Path));
        await Task.WhenAll(creationTimeTasksByChildPath.Values).ConfigureAwait(false);
        
        ephemeralChildrenWithPrefix.RemoveAll(t => creationTimeTasksByChildPath[t.Path].Result == null); // remove all nodes with no creation time (they no longer exist)
        if (ephemeralChildrenWithPrefix.Count == 0) { return Array.Empty<(string, int, string)>(); }

        var oldestChild = ephemeralChildrenWithPrefix.Select((t, index) => (creationTime: creationTimeTasksByChildPath[t.Path].Result!.Value, index))
            .OrderBy(t => t.creationTime)
            .ThenBy(t => t.index)
            .First();
        return ReorderByLowestIndex(oldestChild.index);

        List<(string Path, uint UnsignedSequenceNumber, string Prefix)> GetEphemeralChildrenWithPrefix()
        {
            var result = new List<(string Path, uint UnsignedSequenceNumber, string Prefix)>();
            foreach (var childName in childrenNames)
            {
                int? childSequenceNumber;
                string? childPrefix;
                if (GetSequenceNumberOrDefault(childName, prefix) is { } prefixSequenceNumber)
                {
                    childSequenceNumber = prefixSequenceNumber;
                    childPrefix = prefix;
                }
                else if (alternatePrefix != null
                    && GetSequenceNumberOrDefault(childName, alternatePrefix) is { } alternatePrefixSequenceNumber)
                {
                    childSequenceNumber = alternatePrefixSequenceNumber;
                    childPrefix = alternatePrefix;
                }
                else
                {
                    childSequenceNumber = null;
                    childPrefix = null;
                }

                if (childPrefix != null)
                {
                    result.Add(($"{parentNode.TrimEnd(ZooKeeperPath.Separator)}/{childName}", unchecked((uint)childSequenceNumber!.Value), childPrefix));
                }
            }

            return result;
        }

        (string Path, int SequenceNumber, string Prefix)[] ReorderByLowestIndex(int lowestIndex)
        {
            var result = new (string Path, int SequenceNumber, string Prefix)[ephemeralChildrenWithPrefix.Count];
            for (var i = 0; i < result.Length; ++i)
            {
                var element = ephemeralChildrenWithPrefix[(i + lowestIndex) % result.Length];
                result[i] = (element.Path, unchecked((int)element.UnsignedSequenceNumber), element.Prefix);
            }
            return result;
        }
    }

    /// <summary>
    /// If <paramref name="pathOrName"/> is of the form [.../]prefix{sequence number}, returns the sequence
    /// number. Otherwise, returns null.
    /// </summary>
    internal static int? GetSequenceNumberOrDefault(string pathOrName, string prefix)
    {
        Invariant.Require(prefix.Length > 0);

        // when processing child path names, this should be -1; that means we'll expect the prefix at the start
        var prefixStartIndex = pathOrName.LastIndexOf(ZooKeeperPath.Separator) + 1;
        if (pathOrName.IndexOf(prefix, startIndex: prefixStartIndex) != prefixStartIndex)
        {
            return null;
        }

        // FROM https://zookeeper.apache.org/doc/r3.5.4-beta/zookeeperProgrammers.html#Sequence+Nodes+--+Unique+Naming
        // "The counter has a format of %010d -- that is 10 digits with 0 (zero) padding (the counter is formatted in this way to simplify sorting), 
        // i.e. "<path>0000000001". See Queue Recipe for an example use of this feature. Note: the counter used to store the next sequence number 
        // is a signed int (4bytes) maintained by the parent node, the counter will overflow when incremented beyond 2147483647 (resulting in a name "<path>-2147483648")."
        var counterSuffix = pathOrName.Substring(prefixStartIndex + prefix.Length);
        return (
                (counterSuffix.Length == 10 && counterSuffix[0] != '+') // 10-char number; don't allow leading +
                || (counterSuffix.Length == 11 && counterSuffix[0] == '-') // 11-char number MUST be - and then 10 digits
            )
            && int.TryParse(counterSuffix, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var sequenceNumber)
            ? sequenceNumber
            : default(int?);
    }

    public static async Task<long?> GetNodeCreationTimeAsync(this ZooKeeperConnection connection, string path) =>
        (await connection.ZooKeeper.existsAsync(path).ConfigureAwait(false))?.getCtime();
}
