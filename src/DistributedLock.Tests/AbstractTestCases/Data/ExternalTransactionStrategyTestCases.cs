using NUnit.Framework;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Medallion.Threading.Tests.Data;

public abstract class ExternalTransactionStrategyTestCases<TLockProvider, TDb>
    where TLockProvider : TestingLockProvider<TestingExternalTransactionSynchronizationStrategy<TDb>>, new()
    where TDb : TestingDb, new()
{
    private TLockProvider _lockProvider = default!;

    [SetUp] public void SetUp() => this._lockProvider = new TLockProvider();
    [TearDown] public void TearDown() => this._lockProvider.Dispose();

    [Test]
    public void TestScopedToTransactionOnly()
    {
        this._lockProvider.Strategy.StartAmbient();

        var ambientTransactionLock = this._lockProvider.CreateLock(nameof(TestScopedToTransactionOnly));
        using (ambientTransactionLock.Acquire())
        {
            Assert.That(this._lockProvider.CreateLock(nameof(TestScopedToTransactionOnly)).IsHeld(), Is.True);

            // create a lock of the same type on the underlying connection of the ambient transaction
            using dynamic specificConnectionProvider = Activator.CreateInstance(
                ReplaceGenericParameter(typeof(TLockProvider), this._lockProvider.Strategy.GetType(), typeof(SpecificConnectionStrategy))
            )!;
            specificConnectionProvider.Strategy.Test = this;
            Assert.Catch<InvalidOperationException>(() => ((IDistributedLock)specificConnectionProvider.CreateLock(nameof(TestScopedToTransactionOnly))).Acquire());
        }

        static Type ReplaceGenericParameter(Type type, Type old, Type @new)
        {
            if (type == old) { return @new; }
            if (!type.IsConstructedGenericType) { return type; }

            var newGenericArguments = type.GetGenericArguments()
                .Select(a => ReplaceGenericParameter(a, old, @new))
                .ToArray();
            return type.GetGenericTypeDefinition()
                .MakeGenericType(newGenericArguments);
        }
    }

    /// <summary>
    /// Special strategy designed to allow us to make connection-scoped locks using the same connection as
    /// the ambient transaction from our own <see cref="_lockProvider"/>
    /// </summary>
    private class SpecificConnectionStrategy : TestingDbSynchronizationStrategy<TDb>
    {
        public ExternalTransactionStrategyTestCases<TLockProvider, TDb>? Test { get; set; }

        public override TestingDbConnectionOptions GetConnectionOptions() =>
            new() { Connection = this.Test!._lockProvider.Strategy.AmbientTransaction!.Connection };
    }

    public void TestCloseTransactionLockOnClosedConnectionOrTransaction([Values] bool closeConnection)
    {
        var lockName = closeConnection ? "Connection" : "Transaction";

        var nonAmbientTransactionLock = this._lockProvider.CreateLock(lockName);

        // Disable pooling for the ambient connection. This is important because we want to show that the lock
        // will get released; in reality for a pooled connection in this scenario the lock-holding connection will
        // return to the pool and would get released the next time that connection was fetched from the pool
        this._lockProvider.Strategy.Db.ConnectionStringBuilder["Pooling"] = false;
        this._lockProvider.Strategy.StartAmbient();
        var ambientTransactionLock = this._lockProvider.CreateLock(lockName);

        using var handle = ambientTransactionLock.Acquire();
        Assert.That(nonAmbientTransactionLock.IsHeld(), Is.True);

        if (closeConnection)
        {
            this._lockProvider.Strategy.AmbientTransaction!.Connection!.Dispose();
        }
        else
        {
            this._lockProvider.Strategy.AmbientTransaction!.Dispose();
        }
        Assert.DoesNotThrow(handle.Dispose);

        // now lock can be re-acquired
        Assert.That(nonAmbientTransactionLock.IsHeld(), Is.False);
    }

    [Test]
    public void TestLockOnRolledBackTransaction() => this.TestLockOnCompletedTransactionHelper(t => t.Rollback());

    [Test]
    public void TestLockOnCommittedTransaction() => this.TestLockOnCompletedTransactionHelper(t => t.Commit());

    [Test]
    public void TestLockOnDisposedTransaction() => this.TestLockOnCompletedTransactionHelper(t => t.Dispose());

    private void TestLockOnCompletedTransactionHelper(Action<DbTransaction> complete, [CallerMemberName] string lockName = "")
    {
        var nonAmbientTransactionLock = this._lockProvider.CreateLock(lockName);

        // Disable pooling for the ambient connection. This is important because we want to show that the lock
        // will get released; in reality for a pooled connection in this scenario the lock-holding connection will
        // return to the pool and would get released the next time that connection was fetched from the pool
        this._lockProvider.Strategy.Db.ConnectionStringBuilder["Pooling"] = false;
        this._lockProvider.Strategy.StartAmbient();
        var ambientTransactionLock = this._lockProvider.CreateLock(lockName);

        using var handle = ambientTransactionLock.Acquire();
        Assert.That(nonAmbientTransactionLock.IsHeld(), Is.True);

        complete(this._lockProvider.Strategy.AmbientTransaction!);

        var transactionSupport = this._lockProvider.Strategy.Db.TransactionSupport;
        if (transactionSupport == TransactionSupport.ExplicitParticipation)
        {
            // this will throw because the lock will still be trying to use the transaction and we've ended it
            Assert.Throws<InvalidOperationException>(handle.Dispose);
        }
        else
        {
            Assert.DoesNotThrow(handle.Dispose);
        }

        nonAmbientTransactionLock.IsHeld()
            // explicit participation will fail to release above, so it is still held
            .ShouldEqual(transactionSupport == TransactionSupport.ExplicitParticipation ? true : false);

        if (transactionSupport == TransactionSupport.ImplicitParticipation)
        {
            // If we use transactions implicitly then we can keep using our lock without issue
            // because we're just using the underlyign connection which is still good.
            Assert.DoesNotThrow(() => ambientTransactionLock.Acquire().Dispose());
        }
        else
        {
            // Otherwise we'll fail to use a transaction that has been ended
            Assert.Catch<InvalidOperationException>(() => ambientTransactionLock.Acquire());
        }
    }
}
