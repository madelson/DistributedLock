using Medallion.Threading.Internal.Data;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Oracle
{
    internal class OracleDatabaseConnection : DatabaseConnection
    {
        public const string ApplicationNameIndicatorPrefix = "__DistributedLock.ApplicationName=";

        public OracleDatabaseConnection(IDbConnection connection)
            : base(connection, isExternallyOwned: true)
        {
        }

        public OracleDatabaseConnection(IDbTransaction transaction)
            : base(transaction, isExternallyOwned: true)
        {
        }

        public OracleDatabaseConnection(string connectionString)
            : base(new OracleConnection(connectionString), isExternallyOwned: false)
        {
        }

        // from https://docs.oracle.com/html/E10927_01/OracleCommandClass.htm "this method is a no-op" wrt "Prepare()"
        public override bool ShouldPrepareCommands => false;

        public override bool IsCommandCancellationException(Exception exception) => 
            exception is OracleException oracleException
                // based on https://docs.oracle.com/cd/E85694_01/ODPNT/CommandCancel.htm
                && (oracleException.Number == 01013 || oracleException.Number == 00936 || oracleException.Number == 00604);

        public override Task SleepAsync(TimeSpan sleepTime, CancellationToken cancellationToken, Func<DatabaseCommand, CancellationToken, ValueTask<int>> executor)
        {
            // We should be able to use SYS.DBMS_SESSION.SLEEP, but this is broken because cancellation doesn't seem to work.
            // See https://github.com/oracle/dotnet-db-samples/issues/211
            throw new NotSupportedException();
        }

        public static OracleConnection CreateConnection(string connectionString)
        {
            if (connectionString == null) { throw new ArgumentNullException(connectionString, nameof(connectionString)); }

            // The .NET Oracle provider does not currently support ApplicationName natively. However, that functionality is relied on by many of
            // our tests. As a workaround, we permit the application name to be included in the connection string using a custom encoding scheme.
            // This is only intended to work in tests!
            if (!connectionString.StartsWith(ApplicationNameIndicatorPrefix, StringComparison.Ordinal))
            {
                var firstSeparatorIndex = connectionString.IndexOf(';');
                var applicationName = connectionString.Substring(startIndex: ApplicationNameIndicatorPrefix.Length, length: firstSeparatorIndex - ApplicationNameIndicatorPrefix.Length);
                var connection = new OracleConnection(connectionString.Substring(startIndex: firstSeparatorIndex + 1));
                connection.ClientInfo = applicationName;
                return connection;
            }

            return new OracleConnection(connectionString);
        }
    }
}
