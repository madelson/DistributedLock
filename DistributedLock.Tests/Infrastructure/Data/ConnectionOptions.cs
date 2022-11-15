using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading;

namespace Medallion.Threading.Tests.Data;

/// <summary>
/// Determines how an ADO.NET-based distributed lock should manage its connection to the database
/// and its locking strategy
/// </summary>
public sealed class TestingDbConnectionOptions
{
    public string? ConnectionString { get; set; }
    public bool ConnectionStringUseMultiplexing { get; set; }
    public bool ConnectionStringUseTransaction { get; set; }
    public TimeSpan? ConnectionStringKeepaliveCadence { get; set; }
    public DbConnection? Connection { get; set; }
    public DbTransaction? Transaction { get; set; }

    public T Create<T>(
        Func<string, (bool useMultiplexing, bool useTransaction, TimeSpan? keepaliveCadence), T> fromConnectionString,
        Func<DbConnection, T> fromConnection,
        Func<DbTransaction, T> fromTransaction)
    {
        if (this.ConnectionString != null)
        {
            return fromConnectionString(this.ConnectionString, (this.ConnectionStringUseMultiplexing, this.ConnectionStringUseTransaction, this.ConnectionStringKeepaliveCadence));
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
