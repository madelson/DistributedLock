using Medallion.Threading.Postgres;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Postgres
{
    public class PostgresDistributedReaderWriterLockTest
    {
        [Test]
        public void TestValidatesConstructorArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new PostgresDistributedReaderWriterLock(new(0), default(string)!));
            Assert.Throws<ArgumentNullException>(() => new PostgresDistributedReaderWriterLock(new(0), default(IDbConnection)!));
        }
    }
}
