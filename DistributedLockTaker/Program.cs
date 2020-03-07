using Medallion.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Medallion.Threading.Postgres;
using Medallion.Threading.Tests;
using Medallion.Threading.SqlServer;
using Medallion.Threading.WaitHandles;
#if NET471
using System.Data.SqlClient;
#elif NETCOREAPP3_1
using Microsoft.Data.SqlClient;
#endif

namespace DistributedLockTaker
{
    internal static class Program
    {
        private static readonly string ConnectionString = new SqlConnectionStringBuilder
        {
            DataSource = @".\SQLEXPRESS",
            InitialCatalog = "master",
            IntegratedSecurity = true
        }
        .ConnectionString;

        public static int Main(string[] args)
        {
            var type = args[0];
            var name = args[1];
            IDisposable? handle;
            switch (type)
            {
                case nameof(SqlDistributedLock):
                    handle = new SqlDistributedLock(name, ConnectionString).Acquire();
                    break;
                case nameof(SqlDistributedReaderWriterLock) + "Lock":
                    handle = new SqlDistributedReaderWriterLock(name, ConnectionString).AcquireWriteLock();
                    break;
                case nameof(SqlDistributedSemaphore) + "Lock":
                    handle = new SqlDistributedSemaphore(name, maxCount: 1, connectionString: ConnectionString).Acquire();
                    break;
                case nameof(SqlDistributedSemaphore) + "Lock5":
                    handle = new SqlDistributedSemaphore(name, maxCount: 5, connectionString: ConnectionString).Acquire();
                    break;
                //case "PostgresDistributedLock":
                //    handle = new PostgresDistributedLock(new PostgresAdvisoryLockKey(name), PostgresCredentials.GetConnectionString(Environment.CurrentDirectory)).Acquire();
                //    break;
                case nameof(EventWaitHandleDistributedLock):
                    handle = new EventWaitHandleDistributedLock(name).Acquire();
                    break;
                default:
                    Console.Error.WriteLine($"type: {type}");
                    return 123;
            }

            Console.WriteLine("Acquired");
            Console.Out.Flush();

            if (Console.ReadLine() != "abandon")
            {
                handle.Dispose();
            }

            return 0;
        }
    }
}
