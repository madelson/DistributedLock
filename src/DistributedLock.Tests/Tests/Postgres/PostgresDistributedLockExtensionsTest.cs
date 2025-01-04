using Medallion.Threading.Postgres;
using Npgsql;
using NUnit.Framework;

namespace Medallion.Threading.Tests.Postgres;

internal class PostgresDistributedLockExtensionsTest
{
    [Test]
    public void TestValidatesConstructorArguments()
    {
        Assert.Throws<ArgumentNullException>(() => PostgresDistributedLock.TryAcquireWithTransaction(default, null!));
        Assert.ThrowsAsync<ArgumentNullException>(async () => await PostgresDistributedLock.TryAcquireWithTransactionAsync(default, null!).ConfigureAwait(false));
        Assert.Throws<ArgumentNullException>(() => PostgresDistributedLock.AcquireWithTransaction(default, null!));
        Assert.ThrowsAsync<ArgumentNullException>(async () => await PostgresDistributedLock.AcquireWithTransactionAsync(default, null!).ConfigureAwait(false));
    }

    [Test]
    public async Task TestWorksWithExternalTransaction()
    {
        bool isLockAcquired;

        var key = new PostgresAdvisoryLockKey(0);

        using var connection = new NpgsqlConnection(TestingPostgresDb.DefaultConnectionString);
        await connection.OpenAsync();

        using (var transaction = connection.BeginTransaction())
        {
            PostgresDistributedLock.AcquireWithTransaction(key, transaction);

            isLockAcquired = PostgresDistributedLock.TryAcquireWithTransaction(key, transaction);
            Assert.That(isLockAcquired, Is.False);

            transaction.Rollback();
        }

        using (var transaction = connection.BeginTransaction())
        {
            isLockAcquired = await PostgresDistributedLock.TryAcquireWithTransactionAsync(key, transaction).ConfigureAwait(false);
            Assert.That(isLockAcquired, Is.True);

            Assert.ThrowsAsync<TimeoutException>(async () => await PostgresDistributedLock.AcquireWithTransactionAsync(key, transaction, TimeSpan.FromMilliseconds(10)).ConfigureAwait(false));

            transaction.Commit();
        }
    }
}
