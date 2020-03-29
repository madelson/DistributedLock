using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Data
{
    /// <summary>
    /// Abstraction over an ADO.NET client for a database technology
    /// </summary>
    public interface ITestingDb
    {
        DbConnectionStringBuilder ConnectionStringBuilder { get; }

        // needed since different providers have different names for this key
        public int MaxPoolSize { get; set; }

        int MaxApplicationNameLength { get; }

        bool SupportsTransactionScopedSynchronization { get; }

        DbConnection CreateConnection();

        void ClearPool(DbConnection connection);

        int CountActiveSessions(string applicationName);

        IsolationLevel GetIsolationLevel(DbConnection connection);
    }

    /// <summary>
    /// Interface for the "primary" ADO.NET client for a particular DB backend. For now
    /// this is just used to designate Microsoft.Data.SqlClient vs. System.Data.SqlClient
    /// </summary>
    public interface ITestingPrimaryClientDb : ITestingDb 
    {
        Task KillIdleSessionsAsync(string applicationName, DateTimeOffset expirationDate);
    }
}
