using GBS.Data.GBasedbt;
using Medallion.Threading.Internal.Data;
using System.Data;

namespace Medallion.Threading.GBase;

internal class GBaseDatabaseConnection : DatabaseConnection
{
    public const string ApplicationNameIndicatorPrefix = "__DistributedLock.ApplicationName=";

    // see SleepAsync() for why we need this
    private readonly IDbConnection _innerConnection;

    public GBaseDatabaseConnection(IDbConnection connection)
        : this(connection, isExternallyOwned: true)
    {
    }

    public GBaseDatabaseConnection(IDbTransaction transaction)
        : base(transaction, isExternallyOwned: true)
    {
        this._innerConnection = transaction.Connection;
    }

    public GBaseDatabaseConnection(string connectionString)
        : this(CreateConnection(connectionString), isExternallyOwned: false)
    {
    }

    private GBaseDatabaseConnection(IDbConnection connection, bool isExternallyOwned)
        : base(connection, isExternallyOwned)
    {
        this._innerConnection = connection;
    }

    public override bool ShouldPrepareCommands => false;

    public override bool IsCommandCancellationException(Exception exception) => 
        exception is GbsException gbsException
            && (gbsException.ErrorCode == 01013 || gbsException.ErrorCode == 00936 || gbsException.ErrorCode == 00604);

    public override async Task SleepAsync(TimeSpan sleepTime, CancellationToken cancellationToken, Func<DatabaseCommand, CancellationToken, ValueTask<int>> executor)
    {
        using var sleepCommand = this.CreateCommand();
        sleepCommand.SetCommandText("dbms_lock_sleep(?)");
        sleepCommand.AddParameter("seconds", sleepTime.TotalSeconds);

        try
        {
            await executor(sleepCommand, cancellationToken).ConfigureAwait(false);
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            // GBase doesn't fire StateChange unless the State is observed or the connection is explicitly opened/closed. Therefore, we observe
            // the state on seeing any exception in order to for the event to fire.
            _ = this._innerConnection.State;
            throw;
        }
    }

    public static GbsConnection CreateConnection(string connectionString)
    {
        if (connectionString == null) { throw new ArgumentNullException(connectionString, nameof(connectionString)); }

        // The .NET GBase provider does not currently support ApplicationName natively as a connection string property.
        // However, that functionality is relied on by many of our tests. As a workaround, we permit the application name
        // to be included in the connection string using a custom encoding scheme. This is only intended to work in tests!
        if (connectionString.StartsWith(ApplicationNameIndicatorPrefix, StringComparison.Ordinal))
        {
            var firstSeparatorIndex = connectionString.IndexOf(';');
            var applicationName = connectionString.Substring(startIndex: ApplicationNameIndicatorPrefix.Length, length: firstSeparatorIndex - ApplicationNameIndicatorPrefix.Length);
            // After upgrading the GBase client to 23.6.1, the connection pool sometimes seems to grow beyond what is strictly required.
            // This causes issues if we're tracking connections by name. Therefore, we disable pooling on named connections
            var connection = new GbsConnection(connectionString.Substring(startIndex: firstSeparatorIndex + 1));
            return connection;
        }

        return new GbsConnection(connectionString);
    }
}
