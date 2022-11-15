using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests.WaitHandles;

[SupportsContinuousIntegration]
public sealed class TestingWaitHandleSynchronizationStrategy : TestingSynchronizationStrategy
{
    // since the wait handle won't be collected by the system until all instances of it are closed,
    // we won't see abandoned handles release their tickets until the semaphore is fully abandoned
    public override bool SupportsCrossProcessSingleSemaphoreTicketAbandonment => false;
}
