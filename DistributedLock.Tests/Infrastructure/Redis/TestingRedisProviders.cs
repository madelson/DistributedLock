using Medallion.Threading.Redis;
using Medallion.Threading.Tests.Redis;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Medallion.Threading.Tests.Redis
{
    public abstract class TestingRedisDistributedLockProvider : TestingLockProvider<TestingRedisSynchronizationStrategy>
    {
        private readonly IReadOnlyList<IDatabase> _databases;        

        protected TestingRedisDistributedLockProvider(IEnumerable<IDatabase> databases)
        {
            this._databases = databases.ToArray();
        }

        protected TestingRedisDistributedLockProvider(int serverCount)
            : this(Enumerable.Range(0, serverCount).Select(i => RedisServer.GetDefaultServer(i).Multiplexer.GetDatabase()))
        {
        }

        public override IDistributedLock CreateLockWithExactName(string name)
        {
            var @lock = new RedisDistributedLock(name, this._databases, this.Strategy.Options);
            this.Strategy.RegisterKillHandleAction(
                () => this._databases.Take((this._databases.Count / 2) + 1)
                    .ToList()
                    .ForEach(db => db.KeyDelete(@lock.Key))
            );
            return @lock;
        }

        public override string GetSafeName(string name) => name ?? throw new ArgumentNullException(nameof(name));

        public override string GetCrossProcessLockType() => $"{nameof(RedisDistributedLock)}{this.CrossProcessLockTypeSuffix}";

        protected virtual string CrossProcessLockTypeSuffix => this._databases.Count.ToString();
    }

    public sealed class TestingRedisSingleServerDistributedLockProvider : TestingRedisDistributedLockProvider
    {
        public TestingRedisSingleServerDistributedLockProvider() : base(serverCount: 1) { }
    }

    public sealed class TestingRedis3ServerDistributedLockProvider : TestingRedisDistributedLockProvider
    {
        public TestingRedis3ServerDistributedLockProvider() : base(serverCount: 3) { }
    }

    public sealed class TestingRedis2x1ServerDistributedLockProvider : TestingRedisDistributedLockProvider
    {
        private static readonly IDatabase DeadDatabase;

        static TestingRedis2x1ServerDistributedLockProvider()
        {
            var server = new RedisServer();
            DeadDatabase = server.Multiplexer.GetDatabase();
            using var process = Process.GetProcessById(server.ProcessId);
            process.Kill();
        }

        public TestingRedis2x1ServerDistributedLockProvider()
            : base(Enumerable.Range(0, 2).Select(i => RedisServer.GetDefaultServer(i).Multiplexer.GetDatabase()).Append(DeadDatabase))
        {
        }

        protected override string CrossProcessLockTypeSuffix => "2x1";
    }
}
