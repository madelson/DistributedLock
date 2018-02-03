using Medallion.Threading.Tests.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests
{
    // Contains test classes which implement abstract test cases in all valid combinations. Tests missing from here are discovered by TestSetupTest

    [TestClass]
    public class Core_SystemEngineTest : DistributedLockCoreTestCases<TestingSystemDistributedLockEngine> { }

    [TestClass]
    public class Core_SqlEngine_NoStrategyConnectionStringProviderTest : DistributedLockCoreTestCases<TestingSqlDistributedLockEngine<NoStrategyConnectionStringProvider>> { }

    [TestClass]
    public class Core_SqlEngine_DefaultConnectionStringProviderTest : DistributedLockCoreTestCases<TestingSqlDistributedLockEngine<DefaultConnectionStringProvider>> { }

    [TestClass]
    public class Core_SqlEngine_AzureConnectionStringProviderTest : DistributedLockCoreTestCases<TestingSqlDistributedLockEngine<AzureConnectionStringProvider>> { }

    [TestClass]
    public class Core_SqlEngine_ConnectionBasedConnectionStringProviderTest : DistributedLockCoreTestCases<TestingSqlDistributedLockEngine<ConnectionBasedConnectionStringProvider>> { }

    [TestClass]
    public class Core_SqlEngine_TransactionBasedConnectionStringProviderTest : DistributedLockCoreTestCases<TestingSqlDistributedLockEngine<TransactionBasedConnectionStringProvider>> { }

    [TestClass]
    public class Core_SqlEngine_MultiplexedConnectionStringProviderTest : DistributedLockCoreTestCases<TestingSqlDistributedLockEngine<MultiplexedConnectionStringProvider>> { }

    [TestClass]
    public class Core_SqlEngine_ConnectionProviderTest : DistributedLockCoreTestCases<TestingSqlDistributedLockEngine<ConnectionProvider>> { }

    [TestClass]
    public class Core_SqlEngine_TransactionProviderTest : DistributedLockCoreTestCases<TestingSqlDistributedLockEngine<TransactionProvider>> { }

    [TestClass]
    public class Core_SqlReaderWriterEngine_NoStrategyConnectionStringProviderTest : DistributedLockCoreTestCases<TestingSqlDistributedReaderWriterLockEngine<NoStrategyConnectionStringProvider>> { }

    [TestClass]
    public class Core_SqlReaderWriterEngine_DefaultConnectionStringProviderTest : DistributedLockCoreTestCases<TestingSqlDistributedReaderWriterLockEngine<DefaultConnectionStringProvider>> { }

    [TestClass]
    public class Core_SqlReaderWriterEngine_AzureConnectionStringProviderTest : DistributedLockCoreTestCases<TestingSqlDistributedReaderWriterLockEngine<AzureConnectionStringProvider>> { }

    [TestClass]
    public class Core_SqlReaderWriterEngine_ConnectionBasedConnectionStringProviderTest : DistributedLockCoreTestCases<TestingSqlDistributedReaderWriterLockEngine<ConnectionBasedConnectionStringProvider>> { }

    [TestClass]
    public class Core_SqlReaderWriterEngine_TransactionBasedConnectionStringProviderTest : DistributedLockCoreTestCases<TestingSqlDistributedReaderWriterLockEngine<TransactionBasedConnectionStringProvider>> { }

    [TestClass]
    public class Core_SqlReaderWriterEngine_MultiplexedConnectionStringProviderTest : DistributedLockCoreTestCases<TestingSqlDistributedReaderWriterLockEngine<MultiplexedConnectionStringProvider>> { }

    [TestClass]
    public class Core_SqlReaderWriterEngine_ConnectionProviderTest : DistributedLockCoreTestCases<TestingSqlDistributedReaderWriterLockEngine<ConnectionProvider>> { }

    [TestClass]
    public class Core_SqlReaderWriterEngine_TransactionProviderTest : DistributedLockCoreTestCases<TestingSqlDistributedReaderWriterLockEngine<TransactionProvider>> { }

    [TestClass]
    public class AzureConnectionStrategy_SqlEngineFactoryTest : AzureConnectionStrategyTestCases<TestingSqlDistributedLockEngineFactory> { }

    [TestClass]
    public class AzureConnectionStrategy_SqlReaderWriterEngineFactoryTest : AzureConnectionStrategyTestCases<TestingSqlDistributedReaderWriterLockEngineFactory> { }

    [TestClass]
    public class AzureConnectionStrategy_SqlSemaphoreEngineFactoryTest : AzureConnectionStrategyTestCases<TestingSqlDistributedSemaphoreEngineFactory> { }

    [TestClass]
    public class SqlReaderWriter_NoStrategyConnectionStringProviderTest : SqlDistributedReaderWriterLockTestCases<NoStrategyConnectionStringProvider> { }

    [TestClass]
    public class SqlReaderWriter_DefaultConnectionStringProviderTest : SqlDistributedReaderWriterLockTestCases<DefaultConnectionStringProvider> { }

    [TestClass]
    public class SqlReaderWriter_AzureConnectionStringProviderTest : SqlDistributedReaderWriterLockTestCases<AzureConnectionStringProvider> { }

    [TestClass]
    public class SqlReaderWriter_ConnectionBasedConnectionStringProviderTest : SqlDistributedReaderWriterLockTestCases<ConnectionBasedConnectionStringProvider> { }

    [TestClass]
    public class SqlReaderWriter_TransactionBasedConnectionStringProviderTest : SqlDistributedReaderWriterLockTestCases<TransactionBasedConnectionStringProvider> { }

    [TestClass]
    public class SqlReaderWriter_MultiplexedConnectionStringProviderTest : SqlDistributedReaderWriterLockTestCases<MultiplexedConnectionStringProvider> { }

    [TestClass]
    public class SqlReaderWriter_ConnectionProviderTest : SqlDistributedReaderWriterLockTestCases<ConnectionProvider> { }

    [TestClass]
    public class SqlReaderWriter_TransactionProviderTest : SqlDistributedReaderWriterLockTestCases<TransactionProvider> { }

    [TestClass]
    public class Core_SqlSemaphoreEngine_NoStrategyConnectionStringProviderTest : DistributedLockCoreTestCases<TestingSqlDistributedSemaphoreEngine<NoStrategyConnectionStringProvider>> { }

    [TestClass]
    public class Core_SqlSemaphoreEngine_DefaultConnectionStringProviderTest : DistributedLockCoreTestCases<TestingSqlDistributedSemaphoreEngine<DefaultConnectionStringProvider>> { }

    [TestClass]
    public class Core_SqlSemaphoreEngine_AzureConnectionStringProviderTest : DistributedLockCoreTestCases<TestingSqlDistributedSemaphoreEngine<AzureConnectionStringProvider>> { }

    [TestClass]
    public class Core_SqlSemaphoreEngine_ConnectionBasedConnectionStringProviderTest : DistributedLockCoreTestCases<TestingSqlDistributedSemaphoreEngine<ConnectionBasedConnectionStringProvider>> { }

    [TestClass]
    public class Core_SqlSemaphoreEngine_TransactionBasedConnectionStringProviderTest : DistributedLockCoreTestCases<TestingSqlDistributedSemaphoreEngine<TransactionBasedConnectionStringProvider>> { }

    [TestClass]
    public class Core_SqlSemaphoreEngine_MultiplexedConnectionStringProviderTest : DistributedLockCoreTestCases<TestingSqlDistributedSemaphoreEngine<MultiplexedConnectionStringProvider>> { }

    [TestClass]
    public class Core_SqlSemaphoreEngine_ConnectionProviderTest : DistributedLockCoreTestCases<TestingSqlDistributedSemaphoreEngine<ConnectionProvider>> { }

    [TestClass]
    public class Core_SqlSemaphoreEngine_TransactionProviderTest : DistributedLockCoreTestCases<TestingSqlDistributedSemaphoreEngine<TransactionProvider>> { }

    [TestClass]
    public class Core_MostlyDrainedSqlSemaphoreEngineTest : DistributedLockCoreTestCases<TestingMostlyDrainedSqlSemaphoreDistributedLockEngine> { }

    [TestClass]
    public class SqlSemaphore_NoStrategyConnectionStringProviderTest : SqlDistributedSemaphoreTestCases<NoStrategyConnectionStringProvider> { }

    [TestClass]
    public class SqlSemaphore_DefaultConnectionStringProviderTest : SqlDistributedSemaphoreTestCases<DefaultConnectionStringProvider> { }

    [TestClass]
    public class SqlSemaphore_AzureConnectionStringProviderTest : SqlDistributedSemaphoreTestCases<AzureConnectionStringProvider> { }

    [TestClass]
    public class SqlSemaphore_ConnectionBasedConnectionStringProviderTest : SqlDistributedSemaphoreTestCases<ConnectionBasedConnectionStringProvider> { }

    [TestClass]
    public class SqlSemaphore_TransactionBasedConnectionStringProviderTest : SqlDistributedSemaphoreTestCases<TransactionBasedConnectionStringProvider> { }

    [TestClass]
    public class SqlSemaphore_MultiplexedConnectionStringProviderTest : SqlDistributedSemaphoreTestCases<MultiplexedConnectionStringProvider> { }

    [TestClass]
    public class SqlSemaphore_ConnectionProviderTest : SqlDistributedSemaphoreTestCases<ConnectionProvider> { }

    [TestClass]
    public class SqlSemaphore_TransactionProviderTest : SqlDistributedSemaphoreTestCases<TransactionProvider> { }

    [TestClass]
    public class ExternalConnectionStrategy_SqlEngineFactoryTest : ExternalConnectionStrategyTestCases<TestingSqlDistributedLockEngineFactory> { }

    [TestClass]
    public class ExternalConnectionStrategy_SqlReaderWriterEngineFactoryTest : ExternalConnectionStrategyTestCases<TestingSqlDistributedReaderWriterLockEngineFactory> { }

    [TestClass]
    public class ExternalConnectionStrategy_SqlSemaphoreEngineFactoryTest : ExternalConnectionStrategyTestCases<TestingSqlDistributedSemaphoreEngineFactory> { }

    [TestClass]
    public class ExternalTransactionStrategy_SqlEngineFactoryTest : ExternalTransactionStrategyTestCases<TestingSqlDistributedLockEngineFactory> { }

    [TestClass]
    public class ExternalTransactionStrategy_SqlReaderWriterEngineFactoryTest : ExternalTransactionStrategyTestCases<TestingSqlDistributedReaderWriterLockEngineFactory> { }

    [TestClass]
    public class ExternalTransactionStrategy_SqlSemaphoreEngineFactoryTest : ExternalTransactionStrategyTestCases<TestingSqlDistributedSemaphoreEngineFactory> { }

    [TestClass]
    public class OwnedTransactionStrategy_SqlEngineFactoryTest : OwnedTransactionStrategyTestCases<TestingSqlDistributedLockEngineFactory> { }

    [TestClass]
    public class OwnedTransactionStrategy_SqlReaderWriterEngineFactoryTest : OwnedTransactionStrategyTestCases<TestingSqlDistributedReaderWriterLockEngineFactory> { }

    [TestClass]
    public class OwnedTransactionStrategy_SqlSemaphoreEngineFactoryTest : OwnedTransactionStrategyTestCases<TestingSqlDistributedSemaphoreEngineFactory> { }

    [TestClass]
    public class MultiplexingConnectionStrategy_SqlEngineFactoryTest : MultiplexingConnectionStrategyTestCases<TestingSqlDistributedLockEngineFactory> { }

    [TestClass]
    public class MultiplexingConnectionStrategy_SqlReaderWriterEngineFactoryTest : MultiplexingConnectionStrategyTestCases<TestingSqlDistributedReaderWriterLockEngineFactory> { }

    [TestClass]
    public class MultiplexingConnectionStrategy_SqlSemaphoreEngineFactoryTest : MultiplexingConnectionStrategyTestCases<TestingSqlDistributedSemaphoreEngineFactory> { }

    [TestClass]
    public class SqlSemaphoreSelfDeadlock_ConnectionProviderTest : SqlDistributedSemaphoreSelfDeadlockTestCases<ConnectionProvider> { }

    [TestClass]
    public class SqlSemaphoreSelfDeadlock_TransactionProviderTest : SqlDistributedSemaphoreSelfDeadlockTestCases<TransactionProvider> { }
}
