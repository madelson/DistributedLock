using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Medallion.Threading.FileSystem
{
    internal static class FileNameValidationHelper
    {
        private static readonly HashSet<char> InvalidFileNameChars = new HashSet<char>(Path.GetInvalidFileNameChars());

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

            // handle special relative path names (which should not be allowed)
            switch (name)
            {
                case ".":
                    return ReplacementChar.ToString();
                case "..":
                    return new string(ReplacementChar, count: 2);
                // fall through
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
                // we want to respect the casing of lock names. We normalize to upper case
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
