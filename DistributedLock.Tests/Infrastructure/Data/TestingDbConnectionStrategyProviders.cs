using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Medallion.Threading.Tests.Data
{
    public sealed class TestingOwnConnectionStrategyProvider<TDbProvider> : ITestingDbConnectionStrategyProvider<TDbProvider>
        where TDbProvider : ITestingDbProvider, new()
    {
        public ConnectionStrategy<TDbProvider> GetConnectionStrategy() => new ConnectionStrategy<TDbProvider> { ConnectionString = new TDbProvider().ConnectionString };
        public void PerformAdditionalCleanupForHandleAbandonment() { }
        public void Dispose() { }
    }

    public sealed class TestingOwnTransactionConnectionStrategyProvider<TDbProvider> : ITestingDbConnectionStrategyProvider<TDbProvider>
        where TDbProvider : ITestingDbProvider, new()
    {
        public ConnectionStrategy<TDbProvider> GetConnectionStrategy() => new ConnectionStrategy<TDbProvider>
        { 
            ConnectionString = new TDbProvider().ConnectionString, 
            UseConnectionStringForTransaction = true,
        };

        public void PerformAdditionalCleanupForHandleAbandonment() { }
        public void Dispose() { }
    }

    public sealed class TestingExternalDbConnectionStrategyProvider<TDbProvider, TConnectionProvider> : ITestingDbConnectionStrategyProvider<TDbProvider>
        where TDbProvider : ITestingDbProvider, new()
        where TConnectionProvider : ITestingDbConnectionProvider<TDbProvider>, new()
    {
        private readonly ConcurrentStack<IDbConnection> _connections = new ConcurrentStack<IDbConnection>();

        public ConnectionStrategy<TDbProvider> GetConnectionStrategy()
        {
            var connection = new TConnectionProvider().CreateConnection();
            this._connections.Push(connection);
            connection.Open();
            return new ConnectionStrategy<TDbProvider> { Connection = connection };
        }

        public void PerformAdditionalCleanupForHandleAbandonment()
        {
            var dbProvider = new TConnectionProvider();
            while (this._connections.TryPop(out var connection))
            {
                connection.Dispose();
            }

            // For connection-based locks, disposing the connection may just return it to the pool
            // but won't necessarily free the lock. Therefore we also clear out the pool
            using var clearConnection = new TConnectionProvider().CreateConnection();
            dbProvider.ClearPool(clearConnection);
        }

        public void Dispose()
        {
            var exceptions = new List<Exception>();
            while (this._connections.TryPop(out var connection))
            {
                try { connection.Dispose(); }
                catch (Exception ex) { exceptions.Add(ex); }
            }

            if (exceptions.Any())
            {
                throw new AggregateException(exceptions).Flatten();
            }
        }
    }

    public sealed class TestingExternalDbTransactionStrategyProvider<TDbProvider, TConnectionProvider> : ITestingDbConnectionStrategyProvider<TDbProvider>
        where TDbProvider : ITestingDbProvider, new()
        where TConnectionProvider : ITestingDbConnectionProvider<TDbProvider>, new()
    {
        private readonly TestingExternalDbConnectionStrategyProvider<TDbProvider, TConnectionProvider> _externalConnectionProvider =
            new TestingExternalDbConnectionStrategyProvider<TDbProvider, TConnectionProvider>();

        public ConnectionStrategy<TDbProvider> GetConnectionStrategy()
        {
            return new ConnectionStrategy<TDbProvider> { Transaction = this._externalConnectionProvider.GetConnectionStrategy().Connection!.BeginTransaction() };
        }

        public void PerformAdditionalCleanupForHandleAbandonment() => this._externalConnectionProvider.PerformAdditionalCleanupForHandleAbandonment();

        public void Dispose() => this._externalConnectionProvider.Dispose();
    }
}
