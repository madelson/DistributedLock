using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Medallion.Threading.Tests.Data
{
    /// <summary>
    /// Determines how an ADO.NET-based distributed lock should manage its connection to the database
    /// and its locking strategy
    /// </summary>
    public sealed class TestingDbConnectionOptions
    {
        public string? ConnectionString { get; set; }
        public TestingConnectionStringOptions ConnectionStringOptions { get; set; }
        public DbConnection? Connection { get; set; }
        public DbTransaction? Transaction { get; set; }

        public T Create<T>(
            Func<string, TestingConnectionStringOptions, T> fromConnectionString,
            Func<DbConnection, T> fromConnection,
            Func<DbTransaction, T> fromTransaction)
        {
            if (this.ConnectionString != null)
            {
                return fromConnectionString(this.ConnectionString, this.ConnectionStringOptions);
            }

            if (this.Connection != null)
            {
                return fromConnection(this.Connection);
            }

            if (this.Transaction != null)
            {
                return fromTransaction(this.Transaction);
            }

            throw new InvalidOperationException("should never get here");
        }
    }

    public enum TestingConnectionStringOptions
    {
        None,
        UseTransaction,
        UseMultiplexing,
    }
}
