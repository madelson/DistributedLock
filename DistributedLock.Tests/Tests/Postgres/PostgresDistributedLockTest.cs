//using Medallion.Threading.Postgres;
//using Npgsql;
//using NUnit.Framework;
//using System;
//using System.Collections.Generic;
//using System.Data.Common;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Medallion.Threading.Tests.Postgres
//{
//    public class PostgresDistributedLockTest
//    {
//        [Test]
//        public async Task TestInt64AndInt32PairKeyNamespacesAreDifferent()
//        {
//            var connectionString = TestingPostgresDistributedLockEngine.GetConnectionString();
//            var key1 = new PostgresAdvisoryLockKey(0);
//            var key2 = new PostgresAdvisoryLockKey(0, 0);
//            var @lock1 = new PostgresDistributedLock(key1, connectionString);
//            var @lock2 = new PostgresDistributedLock(key2, connectionString);

//            using var handle1 = await lock1.TryAcquireAsync();
//            Assert.IsNotNull(handle1);

//            using var handle2 = await lock2.TryAcquireAsync();
//            Assert.IsNotNull(handle2);
//        }

//        // todo probably just always needs keepalive
//        [Test]
//        public async Task TestIdleConnectionPruning()
//        {
//            var connectionString = new NpgsqlConnectionStringBuilder(TestingPostgresDistributedLockEngine.GetConnectionString())
//            {
//                ConnectionIdleLifetime = 1,
//                ConnectionPruningInterval = 1,
//                MaxPoolSize = 1,
//                Timeout = 2,
//            }.ConnectionString;

//            var @lock = new PostgresDistributedLock(new PostgresAdvisoryLockKey("IdeConPru"), connectionString);
//            using var handle1 = await @lock.AcquireAsync();
            
//            await Task.Delay(TimeSpan.FromSeconds(.5));

//            using var handle2 = await @lock.TryAcquireAsync(TimeSpan.FromSeconds(5));
//            Assert.IsNotNull(handle2);
//        }

//        // todo idle pruning interval?
//    }
//}
