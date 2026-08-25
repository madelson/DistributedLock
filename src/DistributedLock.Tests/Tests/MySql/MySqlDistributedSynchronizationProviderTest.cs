using Medallion.Threading.MySql;
using Medallion.Threading.Tests.Data;
using NUnit.Framework;
using System.Data;
#if NET7_0_OR_GREATER
using System.Data.Common;
#endif

namespace Medallion.Threading.Tests.MySql;

public class MySqlDistributedSynchronizationProviderTest
{
    [Test]
    public void TestArgumentValidation()
    {
        Assert.Throws<ArgumentNullException>(() => new MySqlDistributedSynchronizationProvider(default(string)!));
        Assert.Throws<ArgumentNullException>(() => new MySqlDistributedSynchronizationProvider(default(IDbConnection)!));
        Assert.Throws<ArgumentNullException>(() => new MySqlDistributedSynchronizationProvider(default(IDbTransaction)!));
#if NET7_0_OR_GREATER
        Assert.Throws<ArgumentNullException>(() => new MySqlDistributedSynchronizationProvider(default(DbDataSource)!));
#endif
    }

    [Test]
    public async Task BasicTest([Values(typeof(TestingMySqlDb), typeof(TestingMariaDbDb))] Type dbType)
    {
        var db = (TestingDb)Activator.CreateInstance(dbType)!;
        var provider = new MySqlDistributedSynchronizationProvider(db.ConnectionString);

        const string LockName = TargetFramework.Current + "ProviderBasicTest";
        await using (await provider.AcquireLockAsync(LockName))
        {
            await using var handle = await provider.TryAcquireLockAsync(LockName);
            Assert.That(handle, Is.Null, db.GetType().Name);
        }
    }
}
