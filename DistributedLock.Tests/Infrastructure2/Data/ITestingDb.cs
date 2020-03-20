using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Medallion.Threading.Tests.Data
{
    /// <summary>
    /// Abstraction over an ADO.NET client for a database technology
    /// </summary>
    public interface ITestingDb
    {
        DbConnectionStringBuilder ConnectionStringBuilder { get; }

        int MaxApplicationNameLength { get; }

        DbConnection CreateConnection();

        void ClearPool(DbConnection connection);

        int CountActiveSessions(string applicationName);
    }

    /// <summary>
    /// Marker interface for the "primary" ADO.NET client for a particular DB backend. For now
    /// this is just used to designate Microsoft.Data.SqlClient vs. System.Data.SqlClient
    /// </summary>
    public interface ITestingPrimaryClientDb : ITestingDb { }
}
