using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Medallion.Threading.Tests.Data
{
    public interface ITestingDbProvider
    {
        string ConnectionString { get; }
    }

    /// <summary>
    /// Separated from <see cref="ITestingDbProvider"/> to support alternative SQL clients
    /// </summary>
    public interface ITestingDbConnectionProvider<TDbProvider>
        where TDbProvider : ITestingDbProvider, new()
    {
        IDbConnection CreateConnection();
        void ClearPool(IDbConnection connection);
    }
}
