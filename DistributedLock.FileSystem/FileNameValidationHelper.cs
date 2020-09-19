using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Medallion.Threading.FileSystem
{
    /// <summary>
    /// Helper class for validating file names and converting lock names to valid file names. 
    /// 
    /// The approach taken here aims to ensure consistent behavior across platforms wherever possible. This is important
    /// to allow for locking files on a shared network drive for example (whether file locks work with the particular NFS
    /// is another issue but at least we don't want to be burned by naming discrepancy).
    /// </summary>
    internal static class FileNameValidationHelper
    {
        /// <summary>
        /// The set of invalid file name characters for Windows, which is a superset of what is invalid on Unix.
        /// For consistent behavior across platforms, we consider all of these characters invalid when constructing
        /// lock file names from lock names. Pulled from dotnet/runtime github
        /// </summary>
        internal static readonly IReadOnlyCollection<char> UnixAndWindowsInvalidFileNameChars = new char[]
        {
            '\"', '<', '>', '|', '\0',
            (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
            (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
            (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
            (char)31, ':', '*', '?', '\\', '/'
        };

        private static readonly HashSet<char> InvalidFileNameChars = new HashSet<char>(
            // we fully expect this union to always be a noop, but I'm putting it here defensively in case
            // this runs on a system that returns an invalid char not captured in the above
            UnixAndWindowsInvalidFileNameChars.Union(Path.GetInvalidFileNameChars())
        );

        /// <summary>
        /// Windows disallows these special names when used exactly or when followed by a . + other characters (an extension).
        /// See https://stackoverflow.com/questions/1976007/what-characters-are-forbidden-in-windows-and-linux-directory-names/1976131
        /// </summary>
        internal static readonly HashSet<string> ReservedWindowsFileNames = new HashSet<string>(
            new[]
            {
                "CON", "CONIN$", "CONOUT$", "PRN", "AUX", "NUL",
                "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
            },
            StringComparer.OrdinalIgnoreCase
        );

        internal const int MaxPathLengthWindows = 260,
            MinFileNameLength = 12,
            MaxPathLengthUnix = 4096,
            FallbackMaxFileNameLength = 64;

        public static string GetLockFileName(DirectoryInfo lockFileDirectory, string name, bool exactName)
        {
            if (lockFileDirectory == null) { throw new ArgumentNullException(nameof(lockFileDirectory)); }
            if (name == null) { throw new ArgumentNullException(nameof(name)); }

            var directoryPath = lockFileDirectory.FullName;
            var directoryHasTrailingSeparator = directoryPath[directoryPath.Length - 1] == Path.DirectorySeparatorChar;

            if (exactName)
            {
                var validName = ConvertToValidName(name);
                if (validName != name) { throw new FormatException($"{nameof(name)}: may not be '', '.', or '..' and may not contain illegal file name characters"); }
                try { return Path.GetFullPath(ToPath(name)); }
                catch (PathTooLongException ex) { throw new FormatException($"Combined {nameof(lockFileDirectory)} and {nameof(name)} is too long", ex); }
            }

            var directoryPathLength = directoryPath.Length + (directoryHasTrailingSeparator ? 0 : 1);

            // if we fit within a traditional (short) windows path, then just use that. This ensures that we get a consistent answer
            // even when different processes that have long file names enabled or not consider the same path
            if (directoryPathLength + MinFileNameLength <= MaxPathLengthWindows
                && TryCreateSafeName(MaxPathLengthWindows - directoryPathLength, out var safeName))
            {
                return safeName;
            }
            
            // our next fallback is to do the same, but assuming the unix max path length which is longer
            if (directoryPathLength + MinFileNameLength <= MaxPathLengthUnix
                && TryCreateSafeName(MaxPathLengthUnix - directoryPathLength, out safeName))
            {
                return safeName;
            }

            // finally, we iteratively consider a small range of reasonable max lengths
            for (var maxNameLength = FallbackMaxFileNameLength; maxNameLength >= MinFileNameLength; --maxNameLength)
            {
                if (TryCreateSafeName(maxNameLength, out safeName))
                {
                    return safeName;
                }
            }

            throw new PathTooLongException($"Unable to construct lock file name because the base directory (length = {directoryPathLength}) does not leave enough room for a {MinFileNameLength} lock name");

            bool TryCreateSafeName(int maxNameLength, out string safeName)
            {
                var safeLockFileName = DistributedLockHelpers.ToSafeName(name, maxNameLength, ConvertToValidName, ComputeHash);
                safeName = ToPath(safeLockFileName);
                return !IsTooLong(safeName);
            }

            string ToPath(string safeLockFileName) => directoryPath
                + (directoryHasTrailingSeparator ? string.Empty : Path.DirectorySeparatorChar.ToString())
                + safeLockFileName;
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

        private static string ConvertToValidName(string name)
        {
            if (name.Length == 0)
            {
                // the suffix here is a random GUID. The idea is to have something fixed that
                // is highly unlikely to collide with anything a user might provide
                return ConvertToValidName("EMPTY_A4ED4E8E7DBB4757AF1BE51A3C139F84");
            }

            const char ReplacementChar = '_';
            Invariant.Require(!InvalidFileNameChars.Contains(ReplacementChar));

            // Handle special relative path names (which should not be allowed). Note that (at least 
            // on Windows) passing more than 2 dots gets treated like "..'. Windows also does not seem
            // to like file name consisting of any mix of dots and/or spaces
            if (name.All(ch => ch == '.' || ch == ' '))
            {
                return ReplacementChar + name;
            }

            // Handle windows reserved names. We do this even on non-windows OS's for consistency across platforms
            var firstDotIndex = name.IndexOf('.');
            if (ReservedWindowsFileNames.Contains(firstDotIndex >= 0 ? name.Substring(0, firstDotIndex) : name))
            {
                return ReplacementChar + name;
            }

            StringBuilder? builder = null;
            var mayNeedCasingMarker = false;
            for (var i = 0; i < name.Length; ++i)
            {
                var @char = name[i];
                if (InvalidFileNameChars.Contains(@char))
                {
                    builder ??= new StringBuilder(name.Length).Append(name, startIndex: 0, count: i);
                    builder.Append(ReplacementChar);
                }
                // We want to respect the casing of lock names. We normalize to upper case
                // per https://docs.microsoft.com/en-us/visualstudio/code-quality/ca1308?view=vs-2019
                else if (builder == null && char.ToUpperInvariant(@char) != @char)
                {
                    mayNeedCasingMarker = true;
                }
                else if (builder != null)
                {
                    builder.Append(@char);
                }
            }

            return mayNeedCasingMarker && builder == null
                ? name + ReplacementChar
                : builder?.ToString() ?? name;
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
}
