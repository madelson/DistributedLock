using Medallion.Threading.Tests.Redis;
using NUnit.Framework;
using StackExchange.Redis;
using StackExchange.Redis.KeyspaceIsolation;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Redis
{
    public abstract class TestingRedisDatabaseProvider
    {
        protected TestingRedisDatabaseProvider(IEnumerable<IDatabase> databases)
        {
            this.Databases = databases.ToArray();
        }

        protected TestingRedisDatabaseProvider(int count)
            : this(Enumerable.Range(0, count).Select(i => RedisServer.GetDefaultServer(i).Multiplexer.GetDatabase()))
        {
        }

        // publicly settable so that callers can alter the dbs in use
        public IReadOnlyList<IDatabase> Databases { get; set; }

        public virtual string CrossProcessLockTypeSuffix => this.Databases.Count.ToString();
    }

    public sealed class TestingRedisSingleDatabaseProvider : TestingRedisDatabaseProvider
    {
        public TestingRedisSingleDatabaseProvider() : base(count: 1) { }
    }

    public sealed class TestingRedisWithKeyPrefixSingleDatabaseProvider : TestingRedisDatabaseProvider
    {
        public TestingRedisWithKeyPrefixSingleDatabaseProvider()
            : base(new[] { RedisServer.GetDefaultServer(0).Multiplexer.GetDatabase().WithKeyPrefix("distributed_locks:") }) { }

        public override string CrossProcessLockTypeSuffix => "1WithPrefix";
    }

    public sealed class TestingRedis3DatabaseProvider : TestingRedisDatabaseProvider
    {
        public TestingRedis3DatabaseProvider() : base(count: 3) { }
    }

    public sealed class TestingRedis2x1DatabaseProvider : TestingRedisDatabaseProvider
    {
        private static readonly IDatabase DeadDatabase;

        static TestingRedis2x1DatabaseProvider()
        {
            var server = new RedisServer(allowAdmin: true);
            DeadDatabase = server.Multiplexer.GetDatabase();
            using var process = Process.GetProcessById(server.ProcessId);
            server.Multiplexer.GetServer($"localhost:{server.Port}").Shutdown(ShutdownMode.Never);
            Assert.IsTrue(process.WaitForExit(5000));
        }

        public TestingRedis2x1DatabaseProvider()
            : base(Enumerable.Range(0, 2).Select(i => RedisServer.GetDefaultServer(i).Multiplexer.GetDatabase()).Append(DeadDatabase))
        {
        }

        public override string CrossProcessLockTypeSuffix => "2x1";
    }
}
