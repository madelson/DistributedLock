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

    [Test]
    public async Task TestTimeoutSettingsRestoredWithExternalTransaction()
    {
        bool isLockAcquired;

        var key = new PostgresAdvisoryLockKey(0);

        using var connection = new NpgsqlConnection(TestingPostgresDb.DefaultConnectionString);
        await connection.OpenAsync();

        using (var transaction = connection.BeginTransaction())
        {
            using var transactionCommand = connection.CreateCommand();
            transactionCommand.Transaction = transaction;

            transactionCommand.CommandText = "SET LOCAL statement_timeout = 1010;SET LOCAL lock_timeout = 510;";
            await transactionCommand.ExecuteNonQueryAsync();

            isLockAcquired = await PostgresDistributedLock.TryAcquireWithTransactionAsync(key, transaction).ConfigureAwait(false);
            Assert.That(isLockAcquired, Is.True);

            (await GetTimeoutAsync("statement_timeout", transactionCommand)).ShouldEqual("1010ms");
            (await GetTimeoutAsync("lock_timeout", transactionCommand)).ShouldEqual("510ms");

            isLockAcquired = PostgresDistributedLock.TryAcquireWithTransaction(key, transaction, TimeSpan.FromMilliseconds(10));
            Assert.That(isLockAcquired, Is.False);

            (await GetTimeoutAsync("statement_timeout", transactionCommand)).ShouldEqual("1010ms");
            (await GetTimeoutAsync("lock_timeout", transactionCommand)).ShouldEqual("510ms");

            transaction.Rollback();

            (await GetTimeoutAsync("statement_timeout", transactionCommand)).ShouldEqual("0");
            (await GetTimeoutAsync("lock_timeout", transactionCommand)).ShouldEqual("0");
        }

        using (var transaction = connection.BeginTransaction())
        {
            using var transactionCommand = connection.CreateCommand();
            transactionCommand.Transaction = transaction;

            transactionCommand.CommandText = "SET LOCAL statement_timeout = 1010;SET LOCAL lock_timeout = 510;";
            await transactionCommand.ExecuteNonQueryAsync();

            await PostgresDistributedLock.AcquireWithTransactionAsync(key, transaction).ConfigureAwait(false);

            (await GetTimeoutAsync("statement_timeout", transactionCommand)).ShouldEqual("1010ms");
            (await GetTimeoutAsync("lock_timeout", transactionCommand)).ShouldEqual("510ms");

            Assert.Throws<TimeoutException>(() => PostgresDistributedLock.AcquireWithTransaction(key, transaction, TimeSpan.FromMilliseconds(10)));

            (await GetTimeoutAsync("statement_timeout", transactionCommand)).ShouldEqual("1010ms");
            (await GetTimeoutAsync("lock_timeout", transactionCommand)).ShouldEqual("510ms");

            transaction.Commit();

            (await GetTimeoutAsync("statement_timeout", transactionCommand)).ShouldEqual("0");
            (await GetTimeoutAsync("lock_timeout", transactionCommand)).ShouldEqual("0");
        }
    }

    
    [Test]
    // Each lock acquisition creates the same named savepoint; this seems like it would create a conflict
    // but it actually works fine in Postgres (see https://www.postgresql.org/docs/current/sql-savepoint.html)
    public async Task TestWorksForMultipleLocksUnderTheSameConnectionWithExternalTransaction()
    {
        var key1 = new PostgresAdvisoryLockKey(1);
        var key2 = new PostgresAdvisoryLockKey(2);

        using var connection = new NpgsqlConnection(TestingPostgresDb.DefaultConnectionString);
        await connection.OpenAsync();

        using (var transaction = connection.BeginTransaction())
        {
            var isFirstLockAcquired = await PostgresDistributedLock.TryAcquireWithTransactionAsync(key1, transaction).ConfigureAwait(false);
            Assert.That(isFirstLockAcquired, Is.True);

            var isSecondLockAcquired = await PostgresDistributedLock.TryAcquireWithTransactionAsync(key2, transaction).ConfigureAwait(false);
            Assert.That(isSecondLockAcquired, Is.True);

            isSecondLockAcquired = await PostgresDistributedLock.TryAcquireWithTransactionAsync(key2, transaction).ConfigureAwait(false);
            Assert.That(isSecondLockAcquired, Is.False);

            transaction.Rollback();
        }
    }

    private static Task<object> GetTimeoutAsync(string timeoutName, NpgsqlCommand command)
    {
        command.CommandText = $"SHOW {timeoutName}";
        return command.ExecuteScalarAsync()!;
    }
}
