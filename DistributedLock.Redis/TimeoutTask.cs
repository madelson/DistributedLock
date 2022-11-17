using Medallion.Threading.Internal;

namespace Medallion.Threading.Redis;

/// <summary>
/// Acts as a <see cref="Task.Delay(TimeSpan, CancellationToken)"/> which is cleaned up when
/// the <see cref="TimeoutTask"/> gets disposed
/// </summary>
internal readonly struct TimeoutTask : IDisposable
{
    private readonly CancellationTokenSource _cleanupTokenSource;
    private readonly CancellationTokenSource? _linkedTokenSource;

    public TimeoutTask(TimeoutValue timeout, CancellationToken cancellationToken)
    {
        this._cleanupTokenSource = new CancellationTokenSource();
        this._linkedTokenSource = cancellationToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this._cleanupTokenSource.Token)
            : null;
        this.Task = Task.Delay(timeout.TimeSpan, this._linkedTokenSource?.Token ?? this._cleanupTokenSource.Token);
    }

    public Task Task { get; }

    public void Dispose()
    {
        try { this._cleanupTokenSource.Cancel(); }
        finally 
        {
            this._linkedTokenSource?.Dispose();
            this._cleanupTokenSource.Dispose(); 
        }
    }
}
