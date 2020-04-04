using System;
using Medallion.Threading.SqlServer;
using Medallion.Threading.WaitHandles;
using Medallion.Threading.Postgres;
using Medallion.Threading.Tests;
#if NET471
using System.Data.SqlClient;
#elif NETCOREAPP3_1
using Microsoft.Data.SqlClient;
#endif

namespace DistributedLockTaker
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            var type = args[0];
            var name = args[1];
            IDisposable? handle;
            switch (type)
            {
                case nameof(SqlDistributedLock):
                    handle = new SqlDistributedLock(name, SqlServerCredentials.ConnectionString).Acquire();
                    break;
                case "Write" + nameof(SqlDistributedReaderWriterLock):
                    handle = new SqlDistributedReaderWriterLock(name, SqlServerCredentials.ConnectionString).AcquireWriteLock();
                    break;
                case nameof(SqlDistributedSemaphore) + "1AsMutex":
                    handle = new SqlDistributedSemaphore(name, maxCount: 1, connectionString: SqlServerCredentials.ConnectionString).Acquire();
                    break;
                case nameof(SqlDistributedSemaphore) + "5AsMutex":
                    handle = new SqlDistributedSemaphore(name, maxCount: 5, connectionString: SqlServerCredentials.ConnectionString).Acquire();
                    break;
                case nameof(PostgresDistributedLock):
                    handle = new PostgresDistributedLock(new PostgresAdvisoryLockKey(name), PostgresCredentials.GetConnectionString(Environment.CurrentDirectory)).Acquire();
                    break;
                case "Write" + nameof(PostgresDistributedReaderWriterLock):
                    handle = new PostgresDistributedReaderWriterLock(new PostgresAdvisoryLockKey(name), PostgresCredentials.GetConnectionString(Environment.CurrentDirectory)).AcquireWriteLock();
                    break;
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
