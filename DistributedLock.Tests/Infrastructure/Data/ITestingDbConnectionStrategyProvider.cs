using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Medallion.Threading.Tests.Data
{
    public interface ITestingDbConnectionStrategyProvider<TDbProvider> : IDisposable
        where TDbProvider : ITestingDbProvider
    {
        ConnectionStrategy<TDbProvider> GetConnectionStrategy();
        void PerformAdditionalCleanupForHandleAbandonment();
    }

    public sealed class ConnectionStrategy<TDbProvider>
        where TDbProvider : ITestingDbProvider
    {
        public string? ConnectionString { get; set; }
        public bool UseConnectionStringForTransaction { get; set; }
        public IDbConnection? Connection { get; set; }
        public IDbTransaction? Transaction { get; set; }

        public T Create<T>(
            Func<string, bool, T> fromConnectionString,
            Func<IDbConnection, T> fromConnection,
            Func<IDbTransaction, T> fromTransaction)
        {
            if (this.ConnectionString != null)
            {
                return fromConnectionString(this.ConnectionString, this.UseConnectionStringForTransaction);
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
}
