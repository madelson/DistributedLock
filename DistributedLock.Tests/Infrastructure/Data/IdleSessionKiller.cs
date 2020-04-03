using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Data
{
    internal class IdleSessionKiller : IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _task;

        public IdleSessionKiller(ITestingPrimaryClientDb db, string applicationName, TimeSpan idleTimeout)
        {
            this._cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = this._cancellationTokenSource.Token;
            this._task = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var expirationDate = DateTimeOffset.Now - idleTimeout;
                    await db.KillSessionsAsync(applicationName, expirationDate);
                    await Task.Delay(TimeSpan.FromTicks(idleTimeout.Ticks / 2), cancellationToken);
                }
            });
        }

        public void Dispose()
        {
            this._cancellationTokenSource.Cancel();

            // wait and swallow any OCE
            try { this._task.Wait(); }
            catch when (this._task.IsCanceled) { }

            this._cancellationTokenSource.Dispose();
        }
    }
}
