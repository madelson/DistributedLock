using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.MySql
{
    /// <summary>
    /// Implements a distributed lock for MySQL or MariaDB based on the GET_LOCK family of functions
    /// </summary>
    public sealed partial class MySqlDistributedLock : IInternalDistributedLock<MySqlDistributedLockHandle>
    {
        /// <summary>
        /// From https://dev.mysql.com/doc/refman/8.0/en/locking-functions.html
        /// </summary>
        internal const int MaxNameLength = 64;

        private readonly IDbDistributedLock _internalLock;

        /// <summary>
        /// Constructs a lock with the given <paramref name="name"/> that connects using the provided <paramref name="connectionString"/> and
        /// <paramref name="options"/>.
        /// 
        /// Unless <paramref name="exactName"/> is specified, <paramref name="name"/> will be escaped/hashed to ensure name validity.
        /// </summary>
        public MySqlDistributedLock(string name, string connectionString, Action<MySqlConnectionOptionsBuilder>? options = null, bool exactName = false)
            : this(name, exactName, n => CreateInternalLock(n, connectionString, options))
        {
        }

        /// <summary>
        /// Constructs a lock with the given <paramref name="name"/> that connects using the provided <paramref name="connection" />.
        /// 
        /// Unless <paramref name="exactName"/> is specified, <paramref name="name"/> will be escaped/hashed to ensure name validity.
        /// </summary>
        public MySqlDistributedLock(string name, IDbConnection connection, bool exactName = false)
            : this(name, exactName, n => CreateInternalLock(n, connection))
        {
        }

        /// <summary>
        /// Constructs a lock with the given <paramref name="name"/> that connects using the connection from the provided <paramref name="transaction" />.
        /// 
        /// NOTE that the lock will not be scoped to the <paramref name="transaction"/> and must still be explicitly released before the transaction ends.
        /// However, this constructor allows the lock to PARTICIPATE in an ongoing transaction on a connection.
        /// 
        /// Unless <paramref name="exactName"/> is specified, <paramref name="name"/> will be escaped/hashed to ensure name validity.
        /// </summary>
        public MySqlDistributedLock(string name, IDbTransaction transaction, bool exactName = false)
            : this(name, exactName, n => CreateInternalLock(n, transaction))
        {
        }

        private MySqlDistributedLock(string name, bool exactName, Func<string, IDbDistributedLock> internalLockFactory)
        {
            if (name == null) { throw new ArgumentNullException(nameof(name)); }

            if (exactName)
            {
                if (name.Length > MaxNameLength) { throw new FormatException($"{nameof(name)}: must be at most {MaxNameLength} characters"); }
                if (name.Length == 0) { throw new FormatException($"{nameof(name)}: must not be empty"); }
                if (name.ToLowerInvariant() != name) { throw new FormatException($"{nameof(name)}: must not container uppercase letters"); }
                this.Name = name;
            }
            else
            {
                this.Name = GetSafeName(name);
            }

            this._internalLock = internalLockFactory(this.Name);
        }

        /// <summary>
        /// Implements <see cref="IDistributedLock.Name"/>
        /// </summary>
        public string Name { get; }

        ValueTask<MySqlDistributedLockHandle?> IInternalDistributedLock<MySqlDistributedLockHandle>.InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken) =>
            this._internalLock.TryAcquireAsync(timeout, new MySqlUserLock(), cancellationToken, contextHandle: null).Wrap(h => new MySqlDistributedLockHandle(h));

        private static string GetSafeName(string name) =>
            ToSafeName(
                name,
                MaxNameLength,
                convertToValidName: s =>
                {
                    if (s.Length == 0) { return "__empty__"; }
                    return s.ToLowerInvariant();
                },
                hash: ComputeHash
            );

        private static string ToSafeName(string name, int maxNameLength, Func<string, string> convertToValidName, Func<byte[], string> hash)
        {
            if (name == null) { throw new ArgumentNullException(nameof(name)); }

            var validBaseLockName = convertToValidName(name);
            if (validBaseLockName == name && validBaseLockName.Length <= maxNameLength)
            {
                return name;
            }

            var nameHash = hash(Encoding.UTF8.GetBytes(name));

            if (nameHash.Length >= maxNameLength)
            {
                return nameHash.Substring(0, length: maxNameLength);
            }

            var prefix = validBaseLockName.Substring(0, Math.Min(validBaseLockName.Length, maxNameLength - nameHash.Length));
            return prefix + nameHash;
        }

        private static string ComputeHash(byte[] bytes)
        {
            using var sha = SHA512.Create();
            var hashBytes = sha.ComputeHash(bytes);

            // We truncate to 160 bits, which is 32 chars of Base32. This should still give us good collision resistance but allows for a 64-char
            // name to include a good portion of the original provided name, which is good for debugging. See
            // https://crypto.stackexchange.com/questions/9435/is-truncating-a-sha512-hash-to-the-first-160-bits-as-secure-as-using-sha1#:~:text=Yes.,time%20is%20still%20pretty%20big
            const int Base32CharBits = 5;
            const int HashLengthInChars = 160 / Base32CharBits;

            // we use Base32 because it is case-insensitive (like MySQL) and a bit more compact than Base16
            // RFC 4648 from https://en.wikipedia.org/wiki/Base32
            const string Base32Alphabet = "abcdefghijklmnopqrstuvwxyz234567";

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

        private static IDbDistributedLock CreateInternalLock(string name, string connectionString, Action<MySqlConnectionOptionsBuilder>? options)
        {
            if (connectionString == null) { throw new ArgumentNullException(nameof(connectionString)); }

            var (keepaliveCadence, useMultiplexing) = MySqlConnectionOptionsBuilder.GetOptions(options);

            if (useMultiplexing)
            {
                return new OptimisticConnectionMultiplexingDbDistributedLock(name, connectionString, MySqlMultiplexedConnectionLockPool.Instance, keepaliveCadence);
            }

            return new DedicatedConnectionOrTransactionDbDistributedLock(name, () => new MySqlDatabaseConnection(connectionString), useTransaction: false, keepaliveCadence);
        }

        static IDbDistributedLock CreateInternalLock(string name, IDbConnection connection)
        {
            if (connection == null) { throw new ArgumentNullException(nameof(connection)); }

            return new DedicatedConnectionOrTransactionDbDistributedLock(name, () => new MySqlDatabaseConnection(connection));
        }

        static IDbDistributedLock CreateInternalLock(string name, IDbTransaction transaction)
        {
            if (transaction == null) { throw new ArgumentNullException(nameof(transaction)); }

            // Note: we pass useTransaction:false here because MYSQL locks are always session-scoped; we only support locking against a transaction
            // so that your lock can participate in the connection.
            return new DedicatedConnectionOrTransactionDbDistributedLock(name, () => new MySqlDatabaseConnection(transaction), useTransaction: false, keepaliveCadence: Timeout.InfiniteTimeSpan);
        }
    }
}
