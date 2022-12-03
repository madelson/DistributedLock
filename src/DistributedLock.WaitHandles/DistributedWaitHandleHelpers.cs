using Medallion.Threading.Internal;

namespace Medallion.Threading.WaitHandles;

internal static class DistributedWaitHandleHelpers
{
    internal const string GlobalPrefix = @"Global\";
    private static readonly TimeoutValue DefaultAbandonmentCheckCadence = TimeSpan.FromSeconds(2);

    // 260 based on LINQPad experimentation
    public const int MaxNameLength = 260;

    public static string GetSafeName(string name)
    {
        if (name == null) { throw new ArgumentNullException(nameof(name)); }

        // Note: the reason we don't add GlobalPrefix inside the ToSafeLockName callback
        // is for backwards compat with the SystemDistributedLock.GetSafeLockName in 1.0.
        // In that version, the global prefix was not exposed as part of the name, and as
        // such it was not accounted for in the hashing performed by ToSafeLockName.

        if (name.StartsWith(GlobalPrefix, StringComparison.Ordinal))
        {
            var suffix = name.Substring(GlobalPrefix.Length);
            var safeSuffix = ConvertToSafeSuffix(suffix);
            return safeSuffix == suffix ? name : GlobalPrefix + safeSuffix;
        }

        return GlobalPrefix + ConvertToSafeSuffix(name);

        static string ConvertToSafeSuffix(string suffix) => DistributedLockHelpers.ToSafeName(
            suffix,
            MaxNameLength - GlobalPrefix.Length,
            s => s.Length == 0 ? "EMPTY" : s.Replace('\\', '_')
        );
    }

    public static string ValidateAndFinalizeName(string name, bool exactName)
    {
        if (exactName)
        {
            ValidateName(name);
            return name;
        }

        return GetSafeName(name);
    }

    private static void ValidateName(string name)
    {
        if (name == null) { throw new ArgumentNullException(nameof(name)); }
        if (name.Length > MaxNameLength) { throw new FormatException($"{nameof(name)}: must be at most {MaxNameLength} characters"); }
        if (!name.StartsWith(GlobalPrefix, StringComparison.Ordinal)) { throw new FormatException($"{nameof(name)}: must start with '{GlobalPrefix}'"); }
        if (name == GlobalPrefix) { throw new FormatException($"{nameof(name)} must not be exactly '{GlobalPrefix}'"); }
        if (name.IndexOf('\\', startIndex: GlobalPrefix.Length) >= 0) { throw new FormatException(nameof(name) + @": must not contain '\'"); }
    }

    public static TimeoutValue ValidateAndFinalizeAbandonmentCheckCadence(TimeSpan? abandonmentCheckCadence)
    {
        if (abandonmentCheckCadence.HasValue)
        {
            var result = new TimeoutValue(abandonmentCheckCadence, nameof(abandonmentCheckCadence));
            if (result.IsZero) { throw new ArgumentOutOfRangeException(nameof(abandonmentCheckCadence), "must not be zero"); }
            return result;
        }
        return DefaultAbandonmentCheckCadence;
    }

    public static TWaitHandle CreateDistributedWaitHandle<TWaitHandle>(
        Func<TWaitHandle> createNew,
        TryOpenExisting<TWaitHandle> tryOpenExisting)
        where TWaitHandle : WaitHandle
    {
        const int MaxTries = 3;
        var tries = 0;

        while (true)
        {
            ++tries;
            try
            {
                return createNew();
            }
            // fallback handling based on https://stackoverflow.com/questions/1784392/my-eventwaithandle-says-access-to-the-path-is-denied-but-its-not
            catch (UnauthorizedAccessException) when (tries <= MaxTries)
            {
                if (tryOpenExisting(out var existing))
                {
                    return existing;
                }
            }

            // if we fail both, we might be in a race. Add in a small random sleep to attempt desynchronization
            Thread.Sleep(new Random(Guid.NewGuid().GetHashCode()).Next(10 * tries));
        }
    }

    public delegate bool TryOpenExisting<TWaitHandle>(out TWaitHandle existing) where TWaitHandle : WaitHandle;

    public static async ValueTask<TWaitHandle?> CreateAndWaitAsync<TWaitHandle>(
        Func<TWaitHandle> createHandle, 
        TimeoutValue abandonmentCheckCadence,
        TimeoutValue timeout, 
        CancellationToken cancellationToken)
        where TWaitHandle : WaitHandle
    {
        var handle = createHandle();
        var cleanup = true;
        try
        {
            if (abandonmentCheckCadence.IsInfinite)
            {
                // no abandonment check: just acquire once
                if (await handle.WaitOneAsync(timeout, cancellationToken).ConfigureAwait(false))
                {
                    cleanup = false;
                    return handle;
                }
                return null;
            }

            if (timeout.IsInfinite)
            {
                // infinite timeout: just loop forever with the abandonment check
                while (true)
                {
                    if (await handle.WaitOneAsync(abandonmentCheckCadence, cancellationToken).ConfigureAwait(false))
                    {
                        cleanup = false;
                        return handle;
                    }

                    // refresh the event in case it was abandoned by the original owner
                    RefreshEvent();
                }
            }

            // fixed timeout: loop in abandonment check chunks
            var elapsedMillis = 0;
            do
            {
                var nextWaitMillis = Math.Min(abandonmentCheckCadence.InMilliseconds, timeout.InMilliseconds - elapsedMillis);
                if (await handle.WaitOneAsync(TimeSpan.FromMilliseconds(nextWaitMillis), cancellationToken).ConfigureAwait(false))
                {
                    cleanup = false;
                    return handle;
                }

                elapsedMillis += nextWaitMillis;

                // refresh the event in case it was abandoned by the original owner
                RefreshEvent();
            }
            while (elapsedMillis < timeout.InMilliseconds);

            return null;
        }
        catch
        {
            // just in case we fail to create a scope or something
            cleanup = true;
            throw;
        }
        finally
        {
            if (cleanup)
            {
                handle.Dispose();
            }
        }

        void RefreshEvent()
        {
            handle.Dispose();
            handle = createHandle();
        }
    }
}
