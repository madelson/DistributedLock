namespace Medallion.Threading.Tests.Postgres
{
    //public sealed class TestingPostgresDistributedLockEngine : TestingDistributedLockEngine
    //{
    //    public static string GetConnectionString() =>
    //        PostgresCredentials.GetConnectionString(TestContext.CurrentContext.TestDirectory);

    //    internal override bool IsReentrant => false;

    //    internal override IDistributedLockOld CreateLockWithExactName(string name) => 
    //        new PostgresDistributedLock(new PostgresAdvisoryLockKey(name), GetConnectionString());

    //    internal override string GetSafeLockName(string name) => PostgresDistributedLock.GetSafeLockName(name).ToString();

    //    internal override void PerformCleanupForLockAbandonment()
    //    {
    //        // since connections are pooled, abandoning a SQL lock won't release the lock right away because the connection
    //        // simply releases back to the pool but doesn't receive an sp_resetconnection until it is re-opened or the pool
    //        // is cleared. Therefore, we clear the pool!

    //        NpgsqlConnection.ClearAllPools();
    //        GC.Collect();
    //        GC.WaitForPendingFinalizers();
    //    }
    //}
}
