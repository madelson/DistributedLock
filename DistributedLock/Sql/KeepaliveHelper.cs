using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    internal sealed class KeepaliveHelper
    {
        // 10-minutes is based on http://searchsqlserver.techtarget.com/feature/Why-you-should-think-twice-about-Windows-Azure-SQL-Database
        // which says Azure closes connections after being idle for 30 minutes
        private static long intervalTicks = TimeSpan.FromMinutes(10).Ticks;

        public static TimeSpan Interval
        {
            get { return TimeSpan.FromTicks(Volatile.Read(ref intervalTicks)); }
            // for testing
            set { Volatile.Write(ref intervalTicks, value.Ticks); }
        }

        private readonly WeakReference<IDbConnection> weakConnection;
        private CancellationTokenSource? cancellationTokenSource;
        private Task? task;

        public KeepaliveHelper(IDbConnection connection)
        {
            this.weakConnection = new WeakReference<IDbConnection>(connection);
        }

        public void Start()
        {
            if (this.task != null) { throw new InvalidOperationException("already started"); } // sanity check

            this.cancellationTokenSource = new CancellationTokenSource();

            this.task = Task.Delay(Interval, this.cancellationTokenSource.Token)
                // set up an explicit delay / continuewith initially to avoid exceptions in the common flow
                .ContinueWith(
                    RunKeepaliveAsync,
                    state: this
                )
                .Unwrap();
        }

        public void Stop()
        {
            if (this.task == null) { throw new InvalidOperationException("already stopped"); } // sanity check

            this.cancellationTokenSource!.Cancel();
            try { this.task.Wait(); }
            finally
            {
                this.cancellationTokenSource.Dispose();
                this.cancellationTokenSource = null;
                this.task = null;
            }
        }

        public async Task StopAsync()
        {
            if (this.task == null) { throw new InvalidOperationException("already stopped"); } // sanity check

            this.cancellationTokenSource!.Cancel();
            try { await this.task.ConfigureAwait(false); }
            finally
            {
                this.cancellationTokenSource.Dispose();
                this.cancellationTokenSource = null;
                this.task = null;
            }
        }

        private static Task RunKeepaliveAsync(Task initialDelay, object state)
        {
            // if the initial delay was canceled, return immediately with a completed task
            if (initialDelay.IsCanceled) { return Task.FromResult(false); }
            // we don't expect this to happen, but if the initial sleep faults then we should just propagate that exception
            if (initialDelay.IsFaulted) { return initialDelay; }
            
            var helper = (KeepaliveHelper)state;
            return RunKeepaliveAsync(helper.weakConnection, helper.cancellationTokenSource!.Token);
        }

        /// <summary>
        /// Executes a keepalive query in a loop. We use a <see cref="WeakReference{T}"/> for the <see cref="IDbConnection"/> so
        /// that an abandoned lock handle will not be held open by this
        /// </summary>
        private static async Task RunKeepaliveAsync(WeakReference<IDbConnection> weakConnection, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (!await ExecuteKeepaliveCommandAsync(weakConnection, cancellationToken).ConfigureAwait(false))
                    {
                        return;
                    }
                }
                catch { } // ignore failures in case they are due to query cancellation or are transient

                try { await Task.Delay(Interval, cancellationToken).ConfigureAwait(false); }
                catch (OperationCanceledException) { } // swallow OCE so we can exit successfully
            }
        }

        private static async Task<bool> ExecuteKeepaliveCommandAsync(WeakReference<IDbConnection> weakConnection, CancellationToken cancellationToken)
        {
            if (!weakConnection.TryGetTarget(out var connection))
            {
                return false;
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT 'Lock connection keepalive'";
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            return true;
        }
    }
}
