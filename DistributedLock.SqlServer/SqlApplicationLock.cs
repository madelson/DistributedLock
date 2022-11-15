using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using System;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.SqlServer;

/// <summary>
/// Implements <see cref="IDbSynchronizationStrategy{TLockCookie}"/> using sp_getapplock
/// </summary>
internal sealed class SqlApplicationLock : IDbSynchronizationStrategy<object>
{
    public const int TimeoutExitCode = -1,
        AlreadyHeldExitCode = 103,
        InvalidUpgradeExitCode = 104;

    public static readonly SqlApplicationLock SharedLock = new SqlApplicationLock(Mode.Shared),
        UpdateLock = new SqlApplicationLock(Mode.Update),
        ExclusiveLock = new SqlApplicationLock(Mode.Exclusive),
        UpgradeLock = new SqlApplicationLock(Mode.Exclusive, isUpgrade: true);

    private static readonly object Cookie = new object();
    private readonly Mode _mode;
    private readonly bool _isUpgrade;

    private SqlApplicationLock(Mode mode, bool isUpgrade = false)
    {
        Invariant.Require(!isUpgrade || mode == Mode.Exclusive);

        this._mode = mode;
        this._isUpgrade = isUpgrade;
    }

    bool IDbSynchronizationStrategy<object>.IsUpgradeable => this._mode == Mode.Update;

    async ValueTask<object?> IDbSynchronizationStrategy<object>.TryAcquireAsync(
        DatabaseConnection connection, 
        string resourceName, 
        TimeoutValue timeout, 
        CancellationToken cancellationToken)
    {
        try
        {
            return await this.ExecuteAcquireCommandAsync(connection, resourceName, timeout, cancellationToken).ConfigureAwait(false) ? Cookie : null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // If the command is canceled, I believe there's a slim chance that acquisition just completed before the cancellation went through.
            // In that case, I'm pretty sure it won't be rolled back. Therefore, to be safe we issue a try-release 
            await ExecuteReleaseCommandAsync(connection, resourceName, isTry: true).ConfigureAwait(false);
            throw;
        }
    }

    ValueTask IDbSynchronizationStrategy<object>.ReleaseAsync(DatabaseConnection connection, string resourceName, object lockCookie) =>
        ExecuteReleaseCommandAsync(connection, resourceName, isTry: false);

    private async Task<bool> ExecuteAcquireCommandAsync(DatabaseConnection connection, string lockName, TimeoutValue timeout, CancellationToken cancellationToken)
    {
        using var command = this.CreateAcquireCommand(connection, lockName, timeout, out var returnValue);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return await ParseExitCodeAsync((int)returnValue.Value, timeout, cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask ExecuteReleaseCommandAsync(DatabaseConnection connection, string lockName, bool isTry)
    {
        using var command = CreateReleaseCommand(connection, lockName, isTry, out var returnValue);
        await command.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
        await ParseExitCodeAsync((int)returnValue.Value, TimeSpan.Zero, CancellationToken.None).ConfigureAwait(false);
    }

    private DatabaseCommand CreateAcquireCommand(
        DatabaseConnection connection, 
        string lockName, 
        TimeoutValue timeout, 
        out IDbDataParameter returnValue)
    {
        var command = connection.CreateCommand();

        if (connection.IsExernallyOwned || this._isUpgrade)
        {
            returnValue = command.AddParameter("Result", type: DbType.Int32, direction: ParameterDirection.Output);

            const string CurrentOwnerMode = "APPLOCK_MODE('public', @Resource, @LockOwner)",
                GetAppLock = "EXEC @Result = dbo.sp_getapplock @Resource=@Resource, @LockMode=@LockMode, @LockOwner=@LockOwner, @LockTimeout=@LockTimeout, @DbPrincipal='public'";
            var alternateOwnerHasLockCheck = connection.IsExernallyOwned && connection.HasTransaction
                ? " OR APPLOCK_MODE('public', @Resource, 'Session') != 'NoLock'"
                : string.Empty;

            if (this._isUpgrade)
            {
                command.SetCommandText(
                    $@"DECLARE @Mode NVARCHAR(32) = {CurrentOwnerMode}
                        IF @Mode = 'NoLock'
                            SET @Result = {InvalidUpgradeExitCode}
                        ELSE IF @Mode != '{GetModeString(Mode.Update)}'{alternateOwnerHasLockCheck}
                            SET @Result = {AlreadyHeldExitCode}
                        ELSE 
                            {GetAppLock}"
                );
            }
            else
            {
                command.SetCommandText(
                    $@"IF {CurrentOwnerMode} != 'NoLock'{alternateOwnerHasLockCheck}
                            SET @Result = {AlreadyHeldExitCode}
                        ELSE
                            {GetAppLock}"
                );
            }
        }
        else
        {
            returnValue = command.AddParameter(type: DbType.Int32, direction: ParameterDirection.ReturnValue);
            command.SetCommandText("dbo.sp_getapplock");
            command.SetCommandType(CommandType.StoredProcedure);
        }
        command.SetTimeout(timeout);

        command.AddParameter("Resource", lockName);
        command.AddParameter("LockMode", GetModeString(this._mode));
        command.AddParameter("LockOwner", connection.HasTransaction ? "Transaction" : "Session");
        command.AddParameter("LockTimeout", timeout.InMilliseconds);
        
        return command;
    }

    private static DatabaseCommand CreateReleaseCommand(DatabaseConnection connection, string lockName, bool isTry, out IDbDataParameter returnValue)
    {
        var command = connection.CreateCommand();
        if (isTry)
        {
            command.SetCommandText(
                @"IF APPLOCK_MODE('public', @Resource, @LockOwner) != 'NoLock'
                        EXEC @Result = dbo.sp_releaseapplock @Resource, @LockOwner
                      ELSE
                        SET @Result = 0"
            );
        }
        else
        {
            command.SetCommandText("dbo.sp_releaseapplock");
            command.SetCommandType(CommandType.StoredProcedure);
        }

        command.AddParameter("Resource", lockName);
        command.AddParameter("LockOwner", connection.HasTransaction ? "Transaction" : "Session");

        if (isTry)
        {
            returnValue = command.AddParameter("Result", type: DbType.Int32, direction: ParameterDirection.Output);
        }
        else
        {
            returnValue = command.AddParameter(type: DbType.Int32, direction: ParameterDirection.ReturnValue);
        } 
        
        return command;
    }

    public static async ValueTask<bool> ParseExitCodeAsync(int exitCode, TimeoutValue timeout, CancellationToken cancellationToken)
    {
        // sp_getapplock exit codes documented at
        // https://msdn.microsoft.com/en-us/library/ms189823.aspx

        switch (exitCode)
        {
            case 0:
            case 1:
                return true;

            case TimeoutExitCode:
                return false;

            case -2: // canceled
                throw new OperationCanceledException(GetErrorMessage(exitCode, "canceled"));
            case -3: // deadlock
                throw new DeadlockException(GetErrorMessage(exitCode, "deadlock"));
            case -999: // parameter / unknown
                throw new ArgumentException(GetErrorMessage(exitCode, "parameter validation or other error"));

            case InvalidUpgradeExitCode: 
                // should never happen unless something goes wrong (e. g. user manually releases the lock on an externally-owned connection)
                throw new InvalidOperationException("Cannot upgrade to an exclusive lock because the update lock is not held");
            case AlreadyHeldExitCode:
                return timeout.IsZero ? false
                    : timeout.IsInfinite ? throw new DeadlockException("Attempted to acquire a lock that is already held on the same connection")
                    : await WaitThenReturnFalseAsync().ConfigureAwait(false);

            default:
                if (exitCode <= 0) { throw new InvalidOperationException(GetErrorMessage(exitCode, "unknown")); }
                return true; // unknown success code
        }

        async ValueTask<bool> WaitThenReturnFalseAsync()
        {
            await SyncViaAsync.Delay(timeout, cancellationToken).ConfigureAwait(false);
            return false;
        }
    }

    private static string GetErrorMessage(int exitCode, string type) => $"The request for the distributed lock failed with exit code {exitCode} ({type})";

    private static string GetModeString(Mode mode) => mode switch
    {
        Mode.Shared => "Shared",
        Mode.Update => "Update",
        Mode.Exclusive => "Exclusive",
        _ => throw new ArgumentException(nameof(mode)),
    };

    private enum Mode
    {
        Shared,
        Update,
        Exclusive,
    }
}
