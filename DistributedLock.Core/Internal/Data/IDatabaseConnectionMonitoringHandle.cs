using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Medallion.Threading.Internal.Data;

internal interface IDatabaseConnectionMonitoringHandle : IDisposable
{
    CancellationToken ConnectionLostToken { get; }
}
