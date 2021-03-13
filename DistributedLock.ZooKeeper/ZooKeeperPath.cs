using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace Medallion.Threading.ZooKeeper
{
    /// <summary>
    /// Represents a path to a ZooKeeper node. The constructor validates that the input is a valid path.
    /// Call <see cref="ToString"/> to get the path value.
    /// </summary>
    public readonly struct ZooKeeperPath : IEquatable<ZooKeeperPath>
    {
        internal const char Separator = '/';

        internal static ZooKeeperPath Root { get; } = new ZooKeeperPath("/");

        private readonly string _path;
        
        /// <summary>
        /// Constructs a new <see cref="ZooKeeperPath"/> based on the given <paramref name="path"/> string.
        /// </summary>
        public ZooKeeperPath(string path) : this(path, checkPath: true) { }
    
        private ZooKeeperPath(string path, bool checkPath, string? paramName = null)
        {
            if (path == null) { throw new ArgumentNullException(paramName ?? nameof(path)); }
            if (checkPath && ValidatePath(path) is { } error)
            {
                throw new FormatException($"{paramName ?? nameof(path)} {error.Reason}{(error.Index.HasValue ? $" (index {error.Index})" : string.Empty)}");
            }
            this._path = path;
        }

        internal ZooKeeperPath? GetDirectory()
        {
            if (this == Root) { return null; }
            var lastSeparatorIndex = this._path.LastIndexOf(Separator);
            return lastSeparatorIndex == 0 ? Root : new ZooKeeperPath(this._path.Substring(0, lastSeparatorIndex), checkPath: false);
        }

        /// <summary>
        /// Returns the path value as a string
        /// </summary>
        public override string ToString() => this._path;

        /// <summary>
        /// Implements equality based on the path string 
        /// </summary>
        public override bool Equals(object obj) => obj is ZooKeeperPath that && this.Equals(that);

        /// <summary>
        /// Implements equality based on the path string 
        /// </summary>
        public bool Equals(ZooKeeperPath that) => this._path == that._path;

        /// <summary>
        /// Implements hashing based on the path string 
        /// </summary>
        public override int GetHashCode() => this._path?.GetHashCode() ?? 0;

        /// <summary>
        /// Implements equality based on the path string 
        /// </summary>
        public static bool operator ==(ZooKeeperPath @this, ZooKeeperPath that) => @this.Equals(that);

        /// <summary>
        /// Implements inequality based on the path string 
        /// </summary>
        public static bool operator !=(ZooKeeperPath @this, ZooKeeperPath that) => !(@this == that);

        internal ZooKeeperPath CreateChildNodeWithSafeName(string name)
        {
            if (name == null) { throw new ArgumentNullException(nameof(name)); }

            var isRoot = this == Root;
            var safeName = DistributedLockHelpers.ToSafeName(
                    name,
                    maxNameLength: int.MaxValue, // no max
                    convertToValidName: ConvertToValidNodeName
                )
                // If ToSafeName adds a hash, it uses Base64 encoding which can include the separator character. We replace
                // with '_' which is not in Base64 so that the output name remains safe without weakening the hash
                .Replace(Separator, '_');
            return new ZooKeeperPath((this == Root ? this._path : (this._path + Separator)) + safeName, checkPath: false);

            string ConvertToValidNodeName(string name)
            {
                // in order to be a valid node name:

                // must not be empty (special-case this because our generic conversion method will map empty to itself)
                if (name.Length == 0) { return "EMPTY"; }

                // must not be ., .., or (this this is root), the reserved path "zookeeper"
                // (see https://zookeeper.apache.org/doc/current/zookeeperProgrammers.html#ch_zkDataModel)
                switch (name)
                {
                    case ".":
                    case "..":
                    case "zookeeper" when isRoot:
                        return name + "_";
                    default:
                        break; // keep going
                }

                if (name.IndexOf(Separator) < 0 // must not contain the path separator
                    && !ValidatePath(Separator + name).HasValue) // "/name" must be a valid path
                {
                    return name;
                }

                var converted = name.ToCharArray();
                for (var i = 0; i < name.Length; ++i)
                {
                    switch (name[i])
                    {
                        // note: we don't have to replace '.' because it is only invalid if '.' or '..' is a full path
                        // segment. Since we'll be appending on a hash, that doesn't matter
                        case Separator: // separator cannot appear in names, only in paths
                        case '\0':
                        case char @char when IsNonNullInvalidPathChar(@char):
                            converted[i] = '_'; // replace with placeholder
                            break;
                    }
                }

                return new string(converted);
            }
        }

        private static (string Reason, int? Index)? ValidatePath(string path)
        {
            // logic based on https://github.com/apache/zookeeper/blob/master/zookeeper-server/src/main/java/org/apache/zookeeper/common/PathUtils.java#L43
            // (cited in https://stackoverflow.com/questions/55463167/node-name-limitations-in-zookeeper)

            if (path.Length == 0) { return ("may not be empty", null); }
            if (path[0] != Separator) { return ("must start with the '/' character", null); }
            if (path.Length == 1) { return null; }
            if (path[path.Length - 1] == Separator) { return ("must not end with the '/' character", path.Length - 1); }

            for (var i = 1; i < path.Length; ++i) 
            {
                // keep in sync with ConvertToValidNodeName()

                switch (path[i])
                {
                    case '\0':
                        return ("may not contain the null character", i);
                    case Separator when path[i - 1] == Separator:
                        return ("may not contain empty segments", i);
                    case '.' when path[i - (path[i - 1] == '.' ? 2 : 1)] == Separator && ((i + 1 == path.Length) || path[i + 1] == Separator):
                        return ("may not be a relative path", i);
                    case char @char when IsNonNullInvalidPathChar(@char):
                        return ("invalid character", i);
                }
            }

            return null;
        }

        private static bool IsNonNullInvalidPathChar(char @char) =>
            @char > '\u0000' && @char <= '\u001f'
            || @char >= '\u007f' && @char <= '\u009F'
            || @char >= '\ud800' && @char <= '\uf8ff'
            || @char >= '\ufff0' && @char <= '\uffff';
    }
}
