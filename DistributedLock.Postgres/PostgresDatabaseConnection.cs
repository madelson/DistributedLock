using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Medallion.Threading.Postgres
{
    internal sealed class PostgresDatabaseConnection : DatabaseConnection
    {
        public PostgresDatabaseConnection(IDbConnection connection, TimeoutValue keepaliveCadence)
            : base(connection, keepaliveCadence, isExternallyOwned: true)
        {
        }

        public PostgresDatabaseConnection(IDbTransaction transaction, TimeoutValue keepaliveCadence)
            : base(transaction, keepaliveCadence, isExternallyOwned: true)
        {
        }

        public PostgresDatabaseConnection(string connectionString, TimeoutValue keepaliveCadence)
            : base(new NpgsqlConnection(connectionString), keepaliveCadence, isExternallyOwned: false)
        {
        }

        // see https://www.npgsql.org/doc/prepare.html
        protected override bool ShouldPrepareCommands => true;

        protected override bool IsCommandCancellationException(Exception exception) =>
            exception is PostgresException postgresException
                // cancellation error code from https://www.postgresql.org/docs/10/errcodes-appendix.html
                && postgresException.SqlState == "57014";
    }
}
