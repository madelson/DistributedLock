﻿using System;
using Medallion.Threading.SqlServer;
using Medallion.Threading.WaitHandles;
using Medallion.Threading.Postgres;
using Medallion.Threading.Tests;
using Medallion.Threading.Azure;
using Azure.Storage.Blobs;
using Medallion.Threading.FileSystem;
using System.IO;
using Medallion.Threading.Redis;
using StackExchange.Redis;
using System.Linq;
using Medallion.Threading;
using System.Collections.Generic;
using Medallion.Threading.ZooKeeper;
using Medallion.Threading.MySql;
using Medallion.Threading.Oracle;

namespace DistributedLockTaker;

internal static class Program
{
    public static int Main(string[] args)
    {
        var type = args[0];
        var name = args[1];
        var connectionString = args[2];
        var connectionStrings = connectionString.Split(new[] { "||" }, StringSplitOptions.None);
        IDisposable? handle;
        switch (type)
        {
            case nameof(SqlDistributedLock):
                handle = new SqlDistributedLock(name, connectionString).Acquire();
                break;
            case "Write" + nameof(SqlDistributedReaderWriterLock):
                handle = new SqlDistributedReaderWriterLock(name, connectionString).AcquireWriteLock();
                break;
            case nameof(SqlDistributedSemaphore) + "1AsMutex":
                handle = new SqlDistributedSemaphore(name, maxCount: 1, connectionString: connectionString).Acquire();
                break;
            case nameof(SqlDistributedSemaphore) + "5AsMutex":
                handle = new SqlDistributedSemaphore(name, maxCount: 5, connectionString: connectionString).Acquire();
                break;
            case nameof(PostgresDistributedLock):
                handle = new PostgresDistributedLock(new PostgresAdvisoryLockKey(name), connectionString).Acquire();
                break;
            case "Write" + nameof(PostgresDistributedReaderWriterLock):
                handle = new PostgresDistributedReaderWriterLock(new PostgresAdvisoryLockKey(name), connectionString).AcquireWriteLock();
                break;
            case nameof(MySqlDistributedLock):
                handle = new MySqlDistributedLock(name, connectionString).Acquire();
                break;
            case "MariaDB" + nameof(MySqlDistributedLock):
                handle = new MySqlDistributedLock(name, connectionString).Acquire();
                break;
            case nameof(OracleDistributedLock):
                handle = new OracleDistributedLock(name, OracleCredentials.GetConnectionString(Environment.CurrentDirectory)).Acquire();
                break;
            case "Write" + nameof(OracleDistributedReaderWriterLock):
                handle = new OracleDistributedReaderWriterLock(name, OracleCredentials.GetConnectionString(Environment.CurrentDirectory)).AcquireWriteLock();
                break;
            case nameof(EventWaitHandleDistributedLock):
                handle = new EventWaitHandleDistributedLock(name).Acquire();
                break;
            case nameof(WaitHandleDistributedSemaphore) + "1AsMutex":
                handle = new WaitHandleDistributedSemaphore(name, maxCount: 1).Acquire();
                break;
            case nameof(WaitHandleDistributedSemaphore) + "5AsMutex":
                handle = new WaitHandleDistributedSemaphore(name, maxCount: 5).Acquire();
                break;
            case nameof(AzureBlobLeaseDistributedLock):
                handle = new AzureBlobLeaseDistributedLock(
                        new BlobClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName, name),
                        o => o.Duration(TimeSpan.FromSeconds(15))
                    )
                    .Acquire();
                break;
            case nameof(FileDistributedLock):
                handle = new FileDistributedLock(new FileInfo(name)).Acquire();
                break;
            case nameof(RedisDistributedLock) + "1":
                ValidateConnectionStringCount(connectionStrings, 1);
                handle = AcquireRedisLock(name, serverCount: 1, connectionStrings);
                break;
            case nameof(RedisDistributedLock) + "3":
                ValidateConnectionStringCount(connectionStrings, 3);
                handle = AcquireRedisLock(name, serverCount: 3, connectionStrings);
                break;
            case nameof(RedisDistributedLock) + "2x1":
                ValidateConnectionStringCount(connectionStrings, 2);
                handle = AcquireRedisLock(name, serverCount: 2, connectionStrings); // we know the last will fail; don't bother (we also don't know its port)
                break;
            case nameof(RedisDistributedLock) + "1WithPrefix":
                ValidateConnectionStringCount(connectionStrings, 1);
                handle = AcquireRedisLock("distributed_locks:" + name, serverCount: 1, connectionStrings);
                break;
            case "Write" + nameof(RedisDistributedReaderWriterLock) + "1":
                ValidateConnectionStringCount(connectionStrings, 1);
                handle = AcquireRedisWriteLock(name, serverCount: 1, connectionStrings);
                break;
            case "Write" + nameof(RedisDistributedReaderWriterLock) + "3":
                ValidateConnectionStringCount(connectionStrings, 3);
                handle = AcquireRedisWriteLock(name, serverCount: 3, connectionStrings);
                break;
            case "Write" + nameof(RedisDistributedReaderWriterLock) + "2x1":
                ValidateConnectionStringCount(connectionStrings, 2);
                handle = AcquireRedisWriteLock(name, serverCount: 2, connectionStrings); // we know the last will fail; don't bother (we also don't know its port)
                break;
            case "Write" + nameof(RedisDistributedReaderWriterLock) + "1WithPrefix":
                ValidateConnectionStringCount(connectionStrings, 1);
                handle = AcquireRedisWriteLock("distributed_locks:" + name, serverCount: 1,connectionStrings);
                break;
            case string _ when type.StartsWith(nameof(RedisDistributedSemaphore)):
                {
                    ValidateConnectionStringCount(connectionStrings, 1);
                    var maxCount = type.EndsWith("1AsMutex") ? 1
                        : type.EndsWith("5AsMutex") ? 5
                        : throw new ArgumentException(type);
                    handle = new RedisDistributedSemaphore(
                        name, 
                        maxCount, 
                        GetRedisDatabases(serverCount: 1, connectionStrings).Single(),
                        // in order to see abandonment work in a reasonable timeframe, use very short expiry
                        options => options.Expiry(TimeSpan.FromSeconds(1))
                            .BusyWaitSleepTime(TimeSpan.FromSeconds(.1), TimeSpan.FromSeconds(.3))
                    ).Acquire();
                    break;
                }
            case nameof(ZooKeeperDistributedLock):
                handle = new ZooKeeperDistributedLock(new ZooKeeperPath(name), ZooKeeperPorts.DefaultConnectionString, options: ZooKeeperOptions).AcquireAsync().Result;
                break;
            case "Write" + nameof(ZooKeeperDistributedReaderWriterLock):
                handle = new ZooKeeperDistributedReaderWriterLock(new ZooKeeperPath(name), ZooKeeperPorts.DefaultConnectionString, options: ZooKeeperOptions).AcquireWriteLockAsync().Result;
                break;
            case string _ when type.StartsWith(nameof(ZooKeeperDistributedSemaphore)):
                {
                    var maxCount = type.EndsWith("1AsMutex") ? 1
                        : type.EndsWith("5AsMutex") ? 5
                        : throw new ArgumentException(type);
                    handle = new ZooKeeperDistributedSemaphore(
                        new ZooKeeperPath(name),
                        maxCount,
                        ZooKeeperPorts.DefaultConnectionString,
                        options: ZooKeeperOptions
                    ).AcquireAsync().Result;
                    break;
                }
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

    private static void ValidateConnectionStringCount(string[] connectionStrings, int expectedCount)
    {
        if (connectionStrings.Length != expectedCount) throw new ArgumentOutOfRangeException("Invalid connection strings count");
    }

    private static IDistributedSynchronizationHandle AcquireRedisLock(string name, int serverCount, string[] connectionStrings) => 
        new RedisDistributedLock(name, GetRedisDatabases(serverCount, connectionStrings), RedisOptions).Acquire();

    private static IDistributedSynchronizationHandle AcquireRedisWriteLock(string name, int serverCount, string[] connectionStrings) =>
        new RedisDistributedReaderWriterLock(name, GetRedisDatabases(serverCount, connectionStrings), RedisOptions).AcquireWriteLock();

    private static IEnumerable<IDatabase> GetRedisDatabases(int serverCount, string[] connectionStrings) => Enumerable.Range(0, serverCount)
        .Select(i => ConnectionMultiplexer.Connect(connectionStrings[i]).GetDatabase());

    private static void RedisOptions(RedisDistributedSynchronizationOptionsBuilder options) => 
        options.Expiry(TimeSpan.FromSeconds(.5)) // short expiry for abandonment testing
            .BusyWaitSleepTime(TimeSpan.FromSeconds(.1), TimeSpan.FromSeconds(.3));

    private static void ZooKeeperOptions(ZooKeeperDistributedSynchronizationOptionsBuilder options) =>
        // use a very short session timeout to support abandonment
        options.SessionTimeout(TimeSpan.FromSeconds(.1));
}
