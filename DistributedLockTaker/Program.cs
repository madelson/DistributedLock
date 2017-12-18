using Medallion.Threading;
using Medallion.Threading.Sql;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedLockTaker.cs
{
    class Program
    {
        private static readonly string ConnectionString = new SqlConnectionStringBuilder
        {
            DataSource = @".\SQLEXPRESS",
            InitialCatalog = "master",
            IntegratedSecurity = true
        }
        .ConnectionString;

        static int Main(string[] args)
        {
            var type = args[0];
            var name = args[1];
            IDisposable handle = null;
            switch (type)
            {
                case "sql":
                    handle = new SqlDistributedLock(name, ConnectionString).Acquire();
                    break;
                case "sqlreaderwriterlock":
                    handle = new SqlDistributedReaderWriterLock(name, ConnectionString).AcquireWriteLock();
                    break;
                case "sqldistributedsemaphore":
                    handle = new SqlDistributedSemaphore(name, maxCount: 1, connectionString: ConnectionString).Acquire();
                    break;
                case "system":
                    handle = new SystemDistributedLock(name).Acquire();
                    break;
                default:
                    return 123;
            }

            if (Console.ReadLine() != "abandon")
            {
                handle.Dispose();
            }

            return 0;
        }
    }
}
