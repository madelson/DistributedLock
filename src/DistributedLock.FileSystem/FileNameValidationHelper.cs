using System.Security.Cryptography;
using System.Text;

namespace Medallion.Threading.FileSystem;

/// <summary>
/// Helper class for validating file names and converting lock names to valid file names.
/// </summary>
internal static class FileNameValidationHelper
{
    // NOTE: our goal here is to ensure consistent behavior across platforms where possible, in case someone is locking a networked file system
    // file. 
    //
    // That means we limit ourselves to just letters, digits and underscores in the name, and we always incorporate a hash component 
    // (which avoids the various Windows "special" file names.
    //
    // Length is another thing we have to consider; on Windows we are dealing with max file name lengths on 255 chars and max path lengths of
    // either 259 chars or 32000 chars, depending on whether long paths are enabled and whether we're on .NET Core or .NET framework. On unix
    // we expect limits of 255 bytes for file names and 4096 bytes for paths. 
    // 
    // Another difference is case-sensitivity: on Windows file names are case-insensitive but we want lock names to be case sensitive.
    //
    // For portability, we prefer to use a fixed name length of 64 chars of the form [clean base name prefix][hash].lock. "clean base name prefix"
    // is a prefix of the orginal name with non letter/digit/underscore chars replaced with underscores. The prefix is as long as possible without going
    // over the overall name limit. "hash" is a Base 32 hash of the UTF8 bytes of the provided name. This gives us case-sensitivity and a minimal chance
    // of collision if the name got truncated or mutated during "cleaning". Finally, the ".lock" extension is a helpful indicator; both this and the name
    // prefix are intended to help with debugging.
    //
    // Sometimes, our base directory will be so long that we can't use this full portable name format. In that case, we fall back to JUST the hash component.
    // If that is still too long, we fall back to just the first 12 chars of the hash. If that is still too long, we give up.

    // Chosen because below this the risk of collisions just seems too high. If someone is picking a base directory so long
    // that names are limited to < 12 chars, it feels like a mistake
    internal const int MinFileNameLength = 12;
    
    // Chosen because this is both less than the 255 chars allowed by Windows and (because we always incorporate a Base 32 hash) less
    // than the 255 bytes allowed by Unix. This is hopefully also short enough to rarely overflow MAX_PATH, even on Windows
    private const int PortableFileNameLength = 64;

    public static string GetLockFileName(DirectoryInfo lockFileDirectory, string name)
    {
        if (lockFileDirectory == null) { throw new ArgumentNullException(nameof(lockFileDirectory)); }
        if (name == null) { throw new ArgumentNullException(nameof(name)); }

        var directoryPath = lockFileDirectory.FullName;
        var directoryPathWithTrailingSeparator = directoryPath[directoryPath.Length - 1] == Path.DirectorySeparatorChar
            ? directoryPath
            : directoryPath + Path.DirectorySeparatorChar;

        var baseName = ConvertToValidBaseName(name);
        var nameHash = ComputeHash(Encoding.UTF8.GetBytes(name));
        const string Extension = ".lock";

        // first, try the full portable name format
        var portableLockFileName = directoryPathWithTrailingSeparator
            + baseName.Substring(0, Math.Min(PortableFileNameLength - nameHash.Length - Extension.Length, baseName.Length))
            + nameHash
            + Extension;
        if (!IsTooLong(portableLockFileName))
        {
            return portableLockFileName;
        }

        // next, try using just the hash as the name
        var hashOnlyFileName = directoryPathWithTrailingSeparator + nameHash;
        if (!IsTooLong(hashOnlyFileName))
        {
            return hashOnlyFileName;
        }

        // finally, try using just a portion of the hash
        var minimumLengthFileName = directoryPathWithTrailingSeparator + nameHash.Substring(0, MinFileNameLength);
        if (!IsTooLong(minimumLengthFileName))
        {
            return minimumLengthFileName;
        }

        throw new PathTooLongException($"Unable to construct lock file name because the base directory (length = {directoryPathWithTrailingSeparator.Length}) does not leave enough room for a {MinFileNameLength} lock name");
    }

    private static bool IsTooLong(string name)
    {
        try 
        {
            Path.GetFullPath(name);
            return false;
        }
        catch (PathTooLongException)
        {
            return true;
        }
    }

    private static string ConvertToValidBaseName(string name)
    {
        const char ReplacementChar = '_';

        StringBuilder? builder = null;
        for (var i = 0; i < name.Length; ++i)
        {
            var @char = name[i];
            if (!char.IsLetterOrDigit(@char) && @char != ReplacementChar)
            {
                builder ??= new StringBuilder(name.Length).Append(name, startIndex: 0, count: i);
                builder.Append(ReplacementChar);
            }
            else if (builder != null)
            {
                builder.Append(@char);
            }
        }

        return builder?.ToString() ?? name;
    }

    // We truncate to 160 bits, which is 32 chars of Base32. This should still give us good collision resistance but allows for a 64-char
    // name to include a good portion of the original provided name, which is good for debugging. See
    // https://crypto.stackexchange.com/questions/9435/is-truncating-a-sha512-hash-to-the-first-160-bits-as-secure-as-using-sha1#:~:text=Yes.,time%20is%20still%20pretty%20big
    private const int Base32CharBits = 5;
    internal const int HashLengthInChars = 160 / Base32CharBits;

    private static string ComputeHash(byte[] bytes)
    {
        using var sha = SHA512.Create();
        var hashBytes = sha.ComputeHash(bytes);

        // we use Base32 because it is case-insensitive (important for windows files) and a bit more compact than Base16
        // RFC 4648 from https://en.wikipedia.org/wiki/Base32
        const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        var chars = new char[HashLengthInChars];
        var byteIndex = 0;
        var bitBuffer = 0;
        var bitsRemaining = 0;
        for (var charIndex = 0; charIndex < chars.Length; ++charIndex)
        {
            if (bitsRemaining < Base32CharBits)
            {
                bitBuffer |= hashBytes[byteIndex++] << bitsRemaining;
                bitsRemaining += 8;
            }
            chars[charIndex] = Base32Alphabet[bitBuffer & 31];
            bitBuffer >>= Base32CharBits;
            bitsRemaining -= Base32CharBits;
        }

        return new string(chars);
    }
}
