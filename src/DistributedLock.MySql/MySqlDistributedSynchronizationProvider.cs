using System.Data;
#if NET7_0_OR_GREATER
using System.Data.Common;
#endif

namespace Medallion.Threading.MySql;

/// <summary>
/// Implements <see cref="IDistributedLockProvider"/> for <see cref="MySqlDistributedLock"/>
/// </summary>
public sealed class MySqlDistributedSynchronizationProvider : IDistributedLockProvider
{
    private readonly Func<string, bool, MySqlDistributedLock> _lockFactory;
    
    /// <summary>
    /// Constructs a provider that connects with <paramref name="connectionString"/> and <paramref name="options"/>.
    /// </summary>
    public MySqlDistributedSynchronizationProvider(string connectionString, Action<MySqlConnectionOptionsBuilder>? options = null)
    {
        if (connectionString == null) { throw new ArgumentNullException(nameof(connectionString)); }

        this._lockFactory = (name, exactName) => new MySqlDistributedLock(name, connectionString, options, exactName);
    }

#if NET7_0_OR_GREATER
    /// <summary>
    /// Constructs a provider that connects with <paramref name="dbDataSource"/> and <paramref name="options"/>.
    /// </summary>
    public MySqlDistributedSynchronizationProvider(DbDataSource dbDataSource, Action<MySqlConnectionOptionsBuilder>? options = null)
    {
        if (dbDataSource == null) { throw new ArgumentNullException(nameof(dbDataSource)); }

        this._lockFactory = (name, exactName) => new MySqlDistributedLock(name, dbDataSource, options, exactName);
    }
#endif

    /// <summary>
    /// Constructs a provider that connects with <paramref name="connection"/>.
    /// </summary>
    public MySqlDistributedSynchronizationProvider(IDbConnection connection)
    {
        if (connection == null) { throw new ArgumentNullException(nameof(connection)); }

        this._lockFactory = (name, exactName) => new MySqlDistributedLock(name, connection, exactName);
    }

    /// <summary>
    /// Constructs a provider that connects with <paramref name="transaction"/>.
    /// </summary>
    public MySqlDistributedSynchronizationProvider(IDbTransaction transaction)
    {
        if (transaction == null) { throw new ArgumentNullException(nameof(transaction)); }

        this._lockFactory = (name, exactName) => new MySqlDistributedLock(name, transaction, exactName);
    }

    /// <summary>
    /// Creates a <see cref="MySqlDistributedLock"/> with the provided <paramref name="name"/>. Unless <paramref name="exactName"/> 
    /// is specified, invalid names will be escaped/hashed.
    /// </summary>
    public MySqlDistributedLock CreateLock(string name, bool exactName = false) => this._lockFactory(name, exactName);

    IDistributedLock IDistributedLockProvider.CreateLock(string name) => this.CreateLock(name);
}
