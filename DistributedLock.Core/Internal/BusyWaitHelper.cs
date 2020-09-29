using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Internal
{
#if DEBUG
    public
#else
    internal
#endif
        static class BusyWaitHelper
    {
        public static async ValueTask<TResult?> WaitAsync<TState, TResult>(
            TState state,
            Func<TState, CancellationToken, ValueTask<TResult?>> tryGetValue, 
            TimeoutValue timeout,
            TimeSpan minSleepTime,
            TimeSpan maxSleepTime,
            CancellationToken cancellationToken)
            where TResult : class
        {
            Invariant.Require(minSleepTime <= maxSleepTime);

            var initialResult = await tryGetValue(state, cancellationToken).ConfigureAwait(false);
            if (initialResult != null || timeout.IsZero)
            {
                return initialResult;
            }

            using var _ = CreateMergedCancellationTokenSourceSource(timeout, cancellationToken, out var mergedCancellationToken);

            var random = new Random(Guid.NewGuid().GetHashCode());
            var sleepRangeMillis = maxSleepTime.TotalMilliseconds - minSleepTime.TotalMilliseconds;
            while (true)
            {
                var sleepTime = minSleepTime + TimeSpan.FromMilliseconds(random.NextDouble() * sleepRangeMillis);
                try
                {
                    await SyncOverAsync.Delay(sleepTime, mergedCancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (IsTimedOut())
                {
                    // if we time out while sleeping, always try one more time with just the regular token
                    return await tryGetValue(state, cancellationToken).ConfigureAwait(false);
                }

                try
                {
                    var result = await tryGetValue(state, mergedCancellationToken).ConfigureAwait(false);
                    if (result != null) { return result; }
                }
                catch (OperationCanceledException) when (IsTimedOut())
                {
                    return null;
                }
            }

            bool IsTimedOut() => 
                mergedCancellationToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested;
        }

        private static IDisposable? CreateMergedCancellationTokenSourceSource(TimeoutValue timeout, CancellationToken cancellationToken, out CancellationToken mergedCancellationToken)
        {
            if (timeout.IsInfinite)
            {
                mergedCancellationToken = cancellationToken;
                return null;
            }

            if (!cancellationToken.CanBeCanceled)
            {
                var timeoutSource = new CancellationTokenSource(millisecondsDelay: timeout.InMilliseconds);
                mergedCancellationToken = timeoutSource.Token;
                return timeoutSource;
            }

            var mergedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            mergedSource.CancelAfter(timeout.InMilliseconds);
            mergedCancellationToken = mergedSource.Token;
            return mergedSource;
        }
    }
}
