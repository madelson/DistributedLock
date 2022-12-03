namespace Medallion.Threading.Internal.Data;

internal interface IDatabaseConnectionMonitoringHandle : IDisposable
{
    CancellationToken ConnectionLostToken { get; }
}
