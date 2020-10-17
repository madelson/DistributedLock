using Medallion.Threading.Internal;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Redis
{
    internal interface IRedisSynchronizationPrimitive
    {
        TimeoutValue AcquireTimeout { get; }
        TimeoutValue ExtensionCadence { get; }
        TimeoutValue Expiry { get; }

        Task<bool> TryAcquireAsync(IDatabaseAsync database);
        bool TryAcquire(IDatabase database);
        Task ReleaseAsync(IDatabaseAsync database, bool fireAndForget);
        void Release(IDatabase database, bool fireAndForget);
        Task<bool> TryExtendAsync(IDatabaseAsync database);
    }
}
