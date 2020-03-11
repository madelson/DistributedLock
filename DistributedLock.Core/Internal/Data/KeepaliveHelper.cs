using Medallion.Threading.Internal;
using System;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Internal.Data
{
    // todo add connection liveness tracking
    /// <summary>
    /// Implements keepalive for a <see cref="DatabaseConnection"/> which is important for certain providers
    /// such as SQL Azure
    /// </summary>
    internal sealed class KeepaliveHelper
    {
        private readonly WeakReference<DatabaseConnection> _weakConnection;
        private readonly TimeoutValue _cadence;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _task;

        public KeepaliveHelper(
            DatabaseConnection connection,
            TimeoutValue cadence)
        {
            this._weakConnection = new WeakReference<DatabaseConnection>(connection);
            this._cadence = cadence;
        }

        public AsyncLock ConnectionLock { get; } = AsyncLock.Create();

        public bool TryStart()
        {
            if (this._task != null)
            {
                return false;
            }

            Invariant.Require(this._cancellationTokenSource == null);
            this._cancellationTokenSource = new CancellationTokenSource();
            this._task = this.RunKeepaliveAsync(this._cancellationTokenSource.Token);
            return true;
        }

        public async ValueTask<bool> TryStopAsync()
        {
            if (this._task != null)
            {
                return false;
            }

            Invariant.Require(this._cancellationTokenSource != null);
            try
            {
                this._cancellationTokenSource!.Cancel();
                if (SyncOverAsync.IsSynchronous) { this._task!.GetAwaiter().GetResult(); }
                else { await this._task!.ConfigureAwait(false); }
            }
            finally
            {
                this._cancellationTokenSource!.Dispose();
                this._cancellationTokenSource = null;
                this._task = null;
            }

            return true;
        }

        private async Task RunKeepaliveAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                await new NonThrowingAwaitable(Task.Delay(this._cadence.InMilliseconds, cancellationToken));
                if (cancellationToken.IsCancellationRequested) { break; }

                // We do a zero-wait try-lock here because if the connection is in-use then someone is querying with it. In that case,
                // There's no need for us to run a keepalive query. Since we are using zero timeout, we don't bother to pass the cancellationToken;
                // this saves us from having to handle cancellation exceptions
                using var handle = await this.ConnectionLock.TryAcquireAsync(TimeSpan.Zero, CancellationToken.None).ConfigureAwait(false);
                if (handle != null)
                {
                    if (!await this.TryRunKeepaliveQueryAsync(cancellationToken).ConfigureAwait(false))
                    {
                        break; // connection was GC'd
                    }
                }
            }
        }

        /// <summary>
        /// Runs a trivial query on <see cref="_weakConnection"/> to keep it open. Returns false if
        /// <see cref="_weakConnection"/> has been garbage-collected
        /// </summary>
        private async ValueTask<bool> TryRunKeepaliveQueryAsync(CancellationToken cancellationToken)
        {
            if (!this._weakConnection.TryGetTarget(out var connection)) { return false; }

            // todo when the user wants a handlelosttoken, we should switch over to sleeping in SQL using WAITFOR or pg_sleep
            using var command = connection.CreateCommand();
            command.SetCommandText("SELECT 1 /* DistributedLock connection keepalive */");
            await new NonThrowingAwaitable(
                command.ExecuteNonQueryAsync(cancellationToken, disallowAsyncCancellation: false, isKeepaliveQuery: true).AsTask()
            );

            return true;
        }

        /// <summary>
        /// Throwing exceptions is slow and our workflow has us canceling tasks in the common case. Using this special awaitable
        /// allows for us to await those tasks without causing a thrown exception
        /// </summary>
        private readonly struct NonThrowingAwaitable : ICriticalNotifyCompletion
        {
            private readonly ConfiguredTaskAwaitable.ConfiguredTaskAwaiter _taskAwaiter;

            public NonThrowingAwaitable(Task task)
            {
                this._taskAwaiter = task.ConfigureAwait(false).GetAwaiter();
            }

            public readonly NonThrowingAwaitable GetAwaiter() => this;

            public bool IsCompleted => this._taskAwaiter.IsCompleted;

            public readonly void GetResult() { } // does NOT call _taskAwaiter.GetResult() since that could throw!

            public void OnCompleted(Action continuation) => this._taskAwaiter.OnCompleted(continuation);
            public void UnsafeOnCompleted(Action continuation) => this._taskAwaiter.UnsafeOnCompleted(continuation);
        }
    }
}
