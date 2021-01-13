using Medallion.Threading.Internal;
using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Medallion.Threading.Postgres
{
    /// <summary>
    /// Acts as the "name" of a distributed lock in Postgres. Consists of one 64-bit value or two 32-bit values (the spaces do not overlap).
    /// See https://www.postgresql.org/docs/12/functions-admin.html#FUNCTIONS-ADVISORY-LOCKS
    /// </summary>
    public readonly struct PostgresAdvisoryLockKey : IEquatable<PostgresAdvisoryLockKey>
    {
        private readonly long _key;
        private readonly KeyEncoding _keyEncoding;

        /// <summary>
        /// Constructs a key from a single 64-bit value. This is a separate key space 
        /// than <see cref="PostgresAdvisoryLockKey(int, int)"/>.
        /// </summary>
        public PostgresAdvisoryLockKey(long key)
        {
            this._key = key;
            this._keyEncoding = KeyEncoding.Int64;
        }

        /// <summary>
        /// Constructs a key from a pair of 32-bit values. This is a separate key space 
        /// than <see cref="PostgresAdvisoryLockKey(long)"/>.
        /// </summary>
        public PostgresAdvisoryLockKey(int key1, int key2)
        {
            this._key = CombineKeys(key1, key2);
            this._keyEncoding = KeyEncoding.Int32Pair;
        }

        /// <summary>
        /// Constructs a key based on a string <paramref name="name"/>.
        /// 
        /// If the string is of the form "{16-digit hex}" or "{8-digit hex},{8-digit hex}", this will be parsed into numeric keys.
        /// 
        /// If the string is an ascii string with 9 or fewer characters, it will be mapped to a key that does not collide with
        /// any other key based on such a string or based on a 32-bit value.
        /// 
        /// Other string names will be rejected unless <paramref name="allowHashing"/> is specified, in which case it will be hashed to
        /// a 64-bit key value.
        /// </summary>
        public PostgresAdvisoryLockKey(string name, bool allowHashing = false)
        {
            if (name == null) { throw new ArgumentNullException(nameof(name)); }

            if (TryEncodeAscii(name, out this._key))
            {
                this._keyEncoding = KeyEncoding.Ascii;
            }
            else if (TryEncodeHashString(name, out this._key, out var hasSeparator))
            {
                this._keyEncoding = hasSeparator ? KeyEncoding.Int32Pair : KeyEncoding.Int64;
            }
            else if (allowHashing)
            {
                this._key = HashString(name);
                this._keyEncoding = KeyEncoding.Int64;
            }
            else
            {
                throw new FormatException($"Name '{name}' could not be encoded as a {nameof(PostgresAdvisoryLockKey)}. Please specify {nameof(allowHashing)} or use one of the following formats:"
                    + $" or (1) a 0-{MaxAsciiLength} character string using only ASCII characters"
                    + $", (2) a {HashStringLength} character hex string, such as the result of Int64.MaxValue.ToString(\"x{HashStringLength}\")"
                    + $", or (3) a 2-part, {SeparatedHashStringLength} character string of the form XXXXXXXX{HashStringSeparator}XXXXXXXX, where the X's are {HashPartLength} hex strings"
                    + $" such as the result of Int32.MaxValue.ToString(\"x{HashPartLength}\")."
                    + " Note that each unique string provided for formats 1 and 2 will map to a unique hash value, with no collisions across formats. Format 3 strings use the same key space as 2.");
            }
        }

        internal bool HasSingleKey => this._keyEncoding == KeyEncoding.Int64;
        
        internal long Key
        {
            get
            {
                Invariant.Require(this.HasSingleKey);
                return this._key;
            }
        }

        // note: we allow calling this even with a single key, since for 
        // pg_locks lookups we have to split the key anyway
        internal (int key1, int key2) Keys => SplitKeys(this._key);

        /// <summary>
        /// Implements <see cref="IEquatable{T}.Equals(T)"/>
        /// </summary>
        public bool Equals(PostgresAdvisoryLockKey that) => this.ToTuple().Equals(that.ToTuple());

        /// <summary>
        /// Implements <see cref="Object.Equals(object)"/>
        /// </summary>
        public override bool Equals(object obj) => obj is PostgresAdvisoryLockKey that && this.Equals(that);

        /// <summary>
        /// Implements <see cref="Object.GetHashCode"/>
        /// </summary>
        public override int GetHashCode() => this.ToTuple().GetHashCode();

        /// <summary>
        /// Provides equality based on <see cref="Equals(PostgresAdvisoryLockKey)"/>
        /// </summary>
        public static bool operator ==(PostgresAdvisoryLockKey a, PostgresAdvisoryLockKey b) => a.Equals(b);
        /// <summary>
        /// Provides inequality based on <see cref="Equals(PostgresAdvisoryLockKey)"/>
        /// </summary>
        public static bool operator !=(PostgresAdvisoryLockKey a, PostgresAdvisoryLockKey b) => !(a == b);

        private (long, bool) ToTuple() => (this._key, this.HasSingleKey);

        /// <summary>
        /// Returns a string representation of the key that can be round-tripped through
        /// <see cref="PostgresAdvisoryLockKey(String, Boolean)"/>
        /// </summary>
        public override string ToString() => this._keyEncoding switch
        {
            KeyEncoding.Int64 => ToHashString(this._key),
            KeyEncoding.Int32Pair => ToHashString(SplitKeys(this._key)),
            KeyEncoding.Ascii => ToAsciiString(this._key),
            _ => throw new InvalidOperationException()
        };

        private static long CombineKeys(int key1, int key2) => unchecked(((long)key1 << (8 * sizeof(int))) | (uint)key2);
        private static (int key1, int key2) SplitKeys(long key) => ((int)(key >> (8 * sizeof(int))), unchecked((int)(key & uint.MaxValue)));

        #region ---- Ascii ----
        // The ASCII encoding works as follows:
        // Each ASCII char is 7 bits allowing for 9 chars = 63 bits in total.
        // In order to differentiate between different-length strings with leading '\0', 
        // we additionally fill the next bit after the string ends with 0. We then fill any
        // remaining bits with 1. Therefore the final 64 bit value is 0-9 7-bit characters followed
        // by 0, followed by N=63-(7*length) 1s

        private const int AsciiCharBits = 7;
        private const int MaxAsciiValue = (1 << AsciiCharBits) - 1;
        internal const int MaxAsciiLength = (8 * sizeof(long)) / AsciiCharBits;

        private static bool TryEncodeAscii(string name, out long key)
        {
            if (name.Length > MaxAsciiLength)
            {
                key = default;
                return false;
            }

            // load the chars into result
            var result = 0L;
            foreach (var @char in name)
            {
                if (@char > MaxAsciiValue)
                {
                    key = default;
                    return false;
                }

                result = (result << AsciiCharBits) | @char;
            }

            // add padding
            result <<= 1; // load zero
            for (var i = name.Length; i < MaxAsciiLength; ++i)
            {
                result = (result << AsciiCharBits) | MaxAsciiValue; // load 1s
            }

            key = result;
            return true;
        }

        private static string ToAsciiString(long key)
        {
            // use unsigned to avoid signed shifts
            var remainingKeyBits = unchecked((ulong)key);

            // unload padding 1s to determine length
            var length = MaxAsciiLength;
            while ((remainingKeyBits & MaxAsciiValue) == MaxAsciiValue)
            {
                --length;
                remainingKeyBits >>= AsciiCharBits;
            }
            Invariant.Require((remainingKeyBits & 1) == 0, "last padding bit should be zero");
            remainingKeyBits >>= 1; // unload padding 0

            var chars = new char[length];
            for (var i = length - 1; i >= 0; --i)
            {
                chars[i] = (char)(remainingKeyBits & MaxAsciiValue);
                remainingKeyBits >>= AsciiCharBits;
            }

            return new string(chars, startIndex: 0, length);
        }
        #endregion

        #region ---- Hashing ----
        private const char HashStringSeparator = ',';
        internal const int HashPartLength = 8, // 8-byte hex numbers
            HashStringLength = 16, // 2 hashes
            SeparatedHashStringLength = HashStringLength + 1; // separated by comma

        private static bool TryEncodeHashString(string name, out long key, out bool hasSeparator)
        { 
            if (name.Length == SeparatedHashStringLength && name[HashPartLength] == HashStringSeparator)
            {
                hasSeparator = true;
            }
            else
            {
                hasSeparator = false;

                if (name.Length != HashStringLength)
                {
                    key = default;
                    return false;
                }
            }

            return TryParseHashKeys(name, out key);

            static bool TryParseHashKeys(string text, out long key)
            {
                if (TryParseHashKey(text.Substring(0, HashPartLength), out var key1)
                    && TryParseHashKey(text.Substring(text.Length - HashPartLength), out var key2))
                {
                    key = CombineKeys(key1, key2);
                    return true;
                }

                key = default;
                return false;
            }

            static bool TryParseHashKey(string text, out int key) =>
                int.TryParse(text, NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out key);
        }

        private static long HashString(string name)
        {
            // The hash result from SHA1 is too large, so we have to truncate (recommended practice and does not
            // weaken the hash other than due to using fewer bytes)

            using var sha1 = SHA1.Create();
            var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(name));

            // We don't use BitConverter here because we want to be endianess-agnostic. 
            // However, this code replicates that result on little-endian
            var result = 0L;
            for (var i = sizeof(long) - 1; i >= 0; --i)
            {
                result = (result << 8) | hashBytes[i];
            }
            return result;
        }

        private static string ToHashString((int key1, int key2) keys) => FormattableString.Invariant($"{keys.key1:x8}{HashStringSeparator}{keys.key2:x8}");

        private static string ToHashString(long key) => key.ToString("x16", NumberFormatInfo.InvariantInfo);
        #endregion

        private enum KeyEncoding
        {
            Int64 = 0,
            Int32Pair,
            Ascii,
        }
    }
}
