namespace Medallion.Threading.Tests.Data
{
    public interface ITestingSqlDistributedLockEngineFactory
    {
        TestingDistributedLockEngine Create<TConnectionManagementProvider>()
            where TConnectionManagementProvider : TestingSqlConnectionManagementProvider, new();
    }
}
