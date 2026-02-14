using Medallion.Threading.Internal.Data;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Medallion.Threading.Oracle;

internal class OracleDatabaseConnection : DatabaseConnection
{
    public const string ApplicationNameIndicatorPrefix = "__DistributedLock.ApplicationName=";

    // see SleepAsync() for why we need this
    private readonly IDbConnection _innerConnection;

    public OracleDatabaseConnection(IDbConnection connection)
        : this(connection, isExternallyOwned: true)
    {
    }

    public OracleDatabaseConnection(IDbTransaction transaction)
        : base(transaction, isExternallyOwned: true)
    {
        this._innerConnection = transaction.Connection;
    }

    public OracleDatabaseConnection(string connectionString)
        : this(CreateConnection(connectionString), isExternallyOwned: false)
    {
    }

    private OracleDatabaseConnection(IDbConnection connection, bool isExternallyOwned)
        : base(connection, isExternallyOwned)
    {
        this._innerConnection = connection;
    }

    // from https://docs.oracle.com/html/E10927_01/OracleCommandClass.htm "this method is a no-op" wrt "Prepare()"
    public override bool ShouldPrepareCommands => false;

    public override bool IsCommandCancellationException(Exception exception) =>
        exception is OracleException oracleException
            // based on https://docs.oracle.com/cd/E85694_01/ODPNT/CommandCancel.htm
            && (oracleException.Number == 01013 || oracleException.Number == 00936 || oracleException.Number == 00604);

    public override async Task SleepAsync(TimeSpan sleepTime, CancellationToken cancellationToken, Func<DatabaseCommand, CancellationToken, ValueTask<int>> executor)
    {
        using var sleepCommand = this.CreateCommand();
        sleepCommand.SetCommandText("BEGIN sys.DBMS_SESSION.SLEEP(:seconds); END;");
        sleepCommand.AddParameter("seconds", sleepTime.TotalSeconds);

        try
        {
            await executor(sleepCommand, cancellationToken).ConfigureAwait(false);
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            // Oracle doesn't fire StateChange unless the State is observed or the connection is explicitly opened/closed. Therefore, we observe
            // the state on seeing any exception in order to for the event to fire. See https://github.com/oracle/dotnet-db-samples/issues/226
            _ = this._innerConnection.State;
            throw;
        }
    }

    public static OracleConnection CreateConnection(string connectionString)
    {
        if (connectionString == null) { throw new ArgumentNullException(connectionString, nameof(connectionString)); }

        // The .NET Oracle provider does not currently support ApplicationName natively as a connection string property.
        // However, that functionality is relied on by many of our tests. As a workaround, we permit the application name
        // to be included in the connection string using a custom encoding scheme. This is only intended to work in tests!
        // See https://github.com/oracle/dotnet-db-samples/issues/216 for more context.
        if (connectionString.StartsWith(ApplicationNameIndicatorPrefix, StringComparison.Ordinal))
        {
            var firstSeparatorIndex = connectionString.IndexOf(';');
            var applicationName = connectionString.Substring(startIndex: ApplicationNameIndicatorPrefix.Length, length: firstSeparatorIndex - ApplicationNameIndicatorPrefix.Length);
            // After upgrading the Oracle client to 23.6.1, the connection pool sometimes seems to grow beyond what is strictly required.
            // This causes issues if we're tracking connections by name. Therefore, we disable pooling on named connections
            var connection = new OracleConnection(connectionString.Substring(startIndex: firstSeparatorIndex + 1));
            connection.ConnectionOpen += _ => connection.ClientInfo = applicationName;
            return connection;
        }

        return new OracleConnection(connectionString);
    }
}
