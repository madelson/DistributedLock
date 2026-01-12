using GBS.Data.GBasedbt;
using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using System.Data;

namespace Medallion.Threading.GBase;

/// <summary>
/// Implements <see cref="IDbSynchronizationStrategy{TLockCookie}"/> using GBase's DBMS_LOCK package
/// </summary>
internal class GBaseDbmsLock : IDbSynchronizationStrategy<object>
{
    private const int MaxWaitSeconds = 32767;
    private const int MaxTimeoutSeconds = MaxWaitSeconds - 1;

    public static readonly GBaseDbmsLock SharedLock = new(Mode.Shared),
        UpdateLock = new(Mode.Update),
        ExclusiveLock = new(Mode.Exclusive),
        UpgradeLock = new(Mode.Exclusive, isUpgrade: true);

    private static readonly object Cookie = new();

    private readonly Mode _mode;
    private readonly bool _isUpgrade;

    private GBaseDbmsLock(Mode mode, bool isUpgrade = false)
    {
        Invariant.Require(!isUpgrade || mode == Mode.Exclusive);

        this._mode = mode;
        this._isUpgrade = isUpgrade;
    }

    public bool IsUpgradeable => this._mode == Mode.Update;

    private int ModeSqlConstant
    {
        get
        {
            var modeCode = this._mode switch
            {
                Mode.Shared => 2,           //SS_MODE
                Mode.Update => 5,           //SSX_MODE
                Mode.Exclusive => 6,        //X_MODE
                _ => throw new InvalidOperationException(),
            };
            return modeCode;
        }
    }

    private async Task<string> GetAllocateLockHandle(DatabaseCommand cmd, string resourceName, CancellationToken cancellationToken)
    {
        var lockAllockFunction = "dbms_lock_allocate_unique";
        cmd.SetCommandText($"{lockAllockFunction}");
        cmd.SetCommandType(CommandType.StoredProcedure);

        var pAllocReturn = new GbsParameter();
        pAllocReturn.GbsType = GbsType.Integer;
        pAllocReturn.ParameterName = "returnValue";
        pAllocReturn.Direction = ParameterDirection.ReturnValue;
        cmd.Parameters.Add(pAllocReturn);

        var pAllocLockName = new GbsParameter();
        pAllocLockName.GbsType = GbsType.LVarChar;
        pAllocLockName.ParameterName = "LockName";
        pAllocLockName.Direction = ParameterDirection.Input;
        pAllocLockName.Value = resourceName;
        cmd.Parameters.Add(pAllocLockName);

        var pAllocLockHandle = new GbsParameter();
        pAllocLockHandle.GbsType = GbsType.LVarChar;
        pAllocLockHandle.ParameterName = "LockHandle";
        pAllocLockHandle.Direction = ParameterDirection.Output;
        pAllocLockHandle.Size = 128;
        cmd.Parameters.Add(pAllocLockHandle);

        var pAllocExpir = new GbsParameter();
        pAllocExpir.GbsType = GbsType.Integer;
        pAllocExpir.ParameterName = "expiration_secs";
        pAllocExpir.Direction = ParameterDirection.Input;
        pAllocExpir.Value = 864000;
        cmd.Parameters.Add(pAllocExpir);
        try
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            if (Convert.ToInt32(pAllocReturn.Value) != 0)
            {
                throw new InvalidOperationException($"allocate locked handle failed. rc = {pAllocReturn.Value}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            foreach (GbsError err in ((GbsException)ex).Errors)
            {
                Console.WriteLine("Native Error" + err.NativeError);
            }
            throw ex;
        }

        return (string)pAllocLockHandle!.Value;
    }

    public async ValueTask ReleaseAsync(DatabaseConnection connection, string resourceName, object lockCookie)
    {
        // Since we we don't allow downgrading and therefore "releasing" an upgrade only happens on disposal of the
        // original handle, this can safely be a noop.
        if (this._isUpgrade) { return; }

        using var cmd = connection.CreateCommand();

        string lockHandle = await this.GetAllocateLockHandle(cmd, resourceName, CancellationToken.None).ConfigureAwait(false);
        var releaseFunction = "dbms_lock_release";

        cmd.SetCommandText(releaseFunction);
        cmd.SetCommandType(CommandType.StoredProcedure);
        cmd.Parameters.Clear();

        var pRequestReturnValue = new GbsParameter();
        pRequestReturnValue.GbsType = GbsType.Integer;
        pRequestReturnValue.ParameterName = "returnFake";
        pRequestReturnValue.Direction = ParameterDirection.ReturnValue;
        cmd.Parameters.Add(pRequestReturnValue);

        var pRequestLockHandle = new GbsParameter();
        pRequestLockHandle.GbsType = GbsType.LVarChar;
        pRequestLockHandle.ParameterName = "lockHandle";
        pRequestLockHandle.Direction = ParameterDirection.Input;
        pRequestLockHandle.Value = lockHandle;
        cmd.Parameters.Add(pRequestLockHandle);

        var pResponseReturnOutput = new GbsParameter();
        pResponseReturnOutput.GbsType = GbsType.Integer;
        pResponseReturnOutput.ParameterName = "returnValue";
        pResponseReturnOutput.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(pResponseReturnOutput);

        await cmd.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);

        var returnValue = (int)pResponseReturnOutput.Value;
        if (returnValue != 0)
        {
            throw new InvalidOperationException($"dbms_lock_release returned error code :{returnValue}");
        }
    }

    public async ValueTask<object?> TryAcquireAsync(DatabaseConnection connection, string resourceName, TimeoutValue timeout, CancellationToken cancellationToken)
    {
        using var cmd = connection.CreateCommand();

        string lockHandle = await this.GetAllocateLockHandle(cmd, resourceName, cancellationToken).ConfigureAwait(false);

        var acquireFunction = this._isUpgrade ? "dbms_lock_convert" : "dbms_lock_request";
        cmd.SetCommandText(acquireFunction);
        cmd.SetCommandType(CommandType.StoredProcedure);
        cmd.SetTimeout(timeout);
        cmd.Parameters.Clear();

        var pRequestReturnFake = new GbsParameter();
        pRequestReturnFake.GbsType = GbsType.Integer;
        pRequestReturnFake.ParameterName = "returnFake";
        pRequestReturnFake.Direction = ParameterDirection.ReturnValue;
        cmd.Parameters.Add(pRequestReturnFake);

        var pRequestLockHandle = new GbsParameter();
        pRequestLockHandle.GbsType = GbsType.LVarChar;
        pRequestLockHandle.ParameterName = "lockHandle";
        pRequestLockHandle.Direction = ParameterDirection.Input;
        pRequestLockHandle.Value = lockHandle;
        cmd.Parameters.Add(pRequestLockHandle);

        var pRequestReturnOutput = new GbsParameter();
        pRequestReturnOutput.GbsType = GbsType.Integer;
        pRequestReturnOutput.ParameterName = "returnValue";
        pRequestReturnOutput.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(pRequestReturnOutput);

        var pRequestLockMode = new GbsParameter();
        pRequestLockMode.GbsType = GbsType.Integer;
        pRequestLockMode.ParameterName = "lockMode";
        pRequestLockMode.Direction = ParameterDirection.Input;
        pRequestLockMode.Value = this.ModeSqlConstant;
        cmd.Parameters.Add(pRequestLockMode);

        var pRequestTimeOut = new GbsParameter();
        pRequestTimeOut.GbsType = GbsType.Integer;
        pRequestTimeOut.ParameterName = "timeout";
        pRequestTimeOut.Direction = ParameterDirection.Input;
        pRequestTimeOut.Value = timeout.IsInfinite ? MaxWaitSeconds
                // we could support longer timeouts via looping lock requests, but this doesn't feel particularly valuable and isn't a true longer wait
                // since by looping you fall out of the wait queue
                : timeout.TimeSpan.TotalSeconds > MaxTimeoutSeconds ? throw new ArgumentOutOfRangeException($"Requested non-infinite timeout value '{timeout}' is longer than GBase's allowed max of '{TimeSpan.FromSeconds(MaxTimeoutSeconds)}'")
                : timeout.TimeSpan.TotalSeconds;
        cmd.Parameters.Add(pRequestTimeOut);

        if (this._isUpgrade == false)
        {
            var pRequestCommit = new GbsParameter();
            pRequestCommit.GbsType = GbsType.Boolean;
            pRequestCommit.ParameterName = "release_on_commit";
            pRequestCommit.Direction = ParameterDirection.Input;
            pRequestCommit.Value = false;
            cmd.Parameters.Add(pRequestCommit);
        }

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        var returnValue = (int)pRequestReturnOutput.Value;
        return returnValue switch
        {
            0 => Cookie, // success
            1 => null, // timeout
            2 => throw new DeadlockException(GetErrorMessage("deadlock")),
            3 => throw new InvalidOperationException(GetErrorMessage("parameter error")),
            4 => timeout.IsZero ? null
                : timeout.IsInfinite ? throw new DeadlockException("Attempted to acquire a lock that is already held on the same connection")
                : await WaitThenReturnNullAsync().ConfigureAwait(false),
            5 => throw new InvalidOperationException(GetErrorMessage("illegal lock handle")),
            _ => throw new InvalidOperationException(GetErrorMessage("unknown error code")),
        };

        string GetErrorMessage(string description) =>
            $"{acquireFunction} returned error code {returnValue} ({description})";

        async ValueTask<object?> WaitThenReturnNullAsync()
        {
            await SyncViaAsync.Delay(timeout, cancellationToken).ConfigureAwait(false);
            return null;
        }
    }

    private enum Mode
    {
        Shared,
        Update,
        Exclusive,
    }
}
