using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests.ProcessScoped
{
    [SupportsContinuousIntegration]
    public sealed class TestingProcessScopedSynchronizationStrategy : TestingSynchronizationStrategy
    {
        public override bool SupportsCrossProcess => false; // since we're scoped to a single process
    }
}
