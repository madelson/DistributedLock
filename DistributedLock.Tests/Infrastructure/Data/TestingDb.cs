using Medallion.Threading.Internal;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Data
{
    /// <summary>
    /// Abstraction over an ADO.NET client for a database technology
    /// </summary>
    public abstract class TestingDb
    {
        public abstract DbConnectionStringBuilder ConnectionStringBuilder { get; }

        public virtual string ApplicationName 
        { 
            get => (string)this.ConnectionStringBuilder["Application Name"]; 
            set => this.ConnectionStringBuilder["Application Name"] = value; 
        }

        public string SetUniqueApplicationName(string baseName = "")
        {
            return this.ApplicationName = DistributedLockHelpers.ToSafeName(
                // note: due to retries, we incorporate a GUID here to ensure that we have a fresh connection pool
                $"{(baseName.Length > 0 ? baseName + "_" : string.Empty)}{TestContext.CurrentContext.Test.FullName}_{TargetFramework.Current}_{Guid.NewGuid()}",
                maxNameLength: this.MaxApplicationNameLength,
                s => s
            );
        }

        public virtual string ConnectionString => this.ConnectionStringBuilder.ConnectionString;

        // needed since different providers have different names for this key
        public virtual int MaxPoolSize
        {
            get => (int)this.GetMaxPoolSizeProperty().GetValue(this.ConnectionStringBuilder)!;
            set => this.GetMaxPoolSizeProperty().SetValue(this.ConnectionStringBuilder, value);
        }

        private PropertyInfo GetMaxPoolSizeProperty() => this.ConnectionStringBuilder.GetType()
            .GetProperty("MaxPoolSize", BindingFlags.Public | BindingFlags.Instance)!;
        
        public abstract int MaxApplicationNameLength { get; }

        public abstract TransactionSupport TransactionSupport { get; }

        public abstract DbConnection CreateConnection();

        public void ClearPool(DbConnection connection)
        {
            var clearPoolMethod = connection.GetType().GetMethod("ClearPool", BindingFlags.Public | BindingFlags.Static);
            clearPoolMethod!.Invoke(null, new[] { connection });
        }

        public abstract int CountActiveSessions(string applicationName);

        public abstract IsolationLevel GetIsolationLevel(DbConnection connection);

        public virtual void PrepareForHighContention(ref int maxConcurrentAcquires) { }
    }

    public enum TransactionSupport
    {
        /// <summary>
        /// The lifetime of the lock is tied to the transaction
        /// </summary>
        TransactionScoped,

        /// <summary>
        /// Connection-scoped lifetime, but locking requests will automatically participate in a transaction if the connection has one
        /// </summary>
        ImplicitParticipation,

        /// <summary>
        /// Connection-scoped lifetime, but locking requests will participate in a transaction if one is explicitly provided
        /// </summary>
        ExplicitParticipation,
    }

    /// <summary>
    /// Interface for the "primary" ADO.NET client for a particular DB backend. For now
    /// this is just used to designate Microsoft.Data.SqlClient vs. System.Data.SqlClient
    /// </summary>
    public abstract class TestingPrimaryClientDb : TestingDb 
    {
        public abstract Task KillSessionsAsync(string applicationName, DateTimeOffset? idleSince = null);
    }
}
