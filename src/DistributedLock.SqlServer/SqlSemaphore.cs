using Medallion.Threading.Internal;
using Medallion.Threading.Internal.Data;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace Medallion.Threading.SqlServer;

internal sealed class SqlSemaphore : IDbSynchronizationStrategy<SqlSemaphore.Cookie>
{
    public SqlSemaphore(int maxCount)
    {
        this.MaxCount = maxCount;
    }

    public int MaxCount { get; }

    #region ---- Execution ----
    public async ValueTask<Cookie?> TryAcquireAsync(DatabaseConnection connection, string resourceName, TimeoutValue timeout, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string? markerTableName;

        // when we aren't supporting cancellation, we can use a simplified one-step algorithm. We treat a timeout of
        // zero in the same way: since there is no blocking, we don't need to bother with explicit cancellation support
        if (!cancellationToken.CanBeCanceled || timeout.IsZero)
        {
            using var command = CreateTextCommand(connection, operationTimeout: timeout);
            command.SetCommandText(AcquireNonCancelableQuery.Value);
            this.AddCommonParameters(command, resourceName, timeout: timeout);
            await command.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
            return await ProcessAcquireResultAsync(command.Parameters, timeout, cancellationToken, out markerTableName, out var ticketLockName).ConfigureAwait(false)
                ? new Cookie(ticket: ticketLockName!, markerTable: markerTableName!)
                : null;
        }

        // cancelable case

        using (var command = CreateTextCommand(connection, operationTimeout: timeout))
        {
            command.SetCommandText(AcquireCancelablePreambleQuery.Value);
            this.AddCommonParameters(command, resourceName);
            // preamble is non-cancelable
            await command.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
            if (await ProcessAcquireResultAsync(command.Parameters, timeout, cancellationToken, out markerTableName, out var ticketLockName).ConfigureAwait(false))
            {
                return new Cookie(ticket: ticketLockName!, markerTable: markerTableName!);
            }
        }

        using (var command = CreateTextCommand(connection, operationTimeout: timeout))
        {
            command.SetCommandText(AcquireCancelableQuery.Value);
            this.AddCommonParameters(command, resourceName, timeout: timeout, markerTableName: markerTableName);
            try
            {
                // see comments around disallowAsyncCancellation for why we pass this flag
                await command.ExecuteNonQueryAsync(cancellationToken, disallowAsyncCancellation: true).ConfigureAwait(false);
            }
            catch when (cancellationToken.IsCancellationRequested)
            {
                // if we canceled the query, we need to perform cleanup to make sure we don't leave marker tables or held locks

                using (var cleanupCommand = CreateTextCommand(connection, operationTimeout: TimeSpan.Zero))
                {
                    cleanupCommand.SetCommandText(CancellationCleanupQuery.Value);
                    this.AddCommonParameters(cleanupCommand, resourceName, markerTableName: markerTableName);
                    await cleanupCommand.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
                }

                throw;
            }

            return await ProcessAcquireResultAsync(command.Parameters, timeout, cancellationToken, out markerTableName, out var ticketLockName).ConfigureAwait(false)
                ? new Cookie(ticket: ticketLockName!, markerTable: markerTableName!)
                : null;
        }
    }

    public async ValueTask ReleaseAsync(DatabaseConnection connection, string resourceName, Cookie lockCookie)
    {
        using var command = CreateTextCommand(connection, operationTimeout: Timeout.InfiniteTimeSpan);
        command.SetCommandText(ReleaseQuery.Value);
        this.AddCommonParameters(command, resourceName, markerTableName: lockCookie.MarkerTable, ticketLockName: lockCookie.Ticket);
        await command.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
    }

    bool IDbSynchronizationStrategy<Cookie>.IsUpgradeable => false;

    public sealed class Cookie
    {
        public Cookie(string ticket, string markerTable)
        {
            this.Ticket = ticket ?? throw new ArgumentNullException(nameof(ticket));
            this.MarkerTable = markerTable ?? throw new ArgumentNullException(nameof(markerTable));
        }

        public string Ticket { get; }
        public string MarkerTable { get; }
    }
    #endregion
    
    #region ---- Command Execution ----
    private static ValueTask<bool> ProcessAcquireResultAsync(
        IDataParameterCollection parameters, 
        TimeoutValue timeout,
        CancellationToken cancellationToken,
        out string? markerTableName, 
        out string? ticketLockName)
    {
        var resultCode = (int)((IDbDataParameter)parameters[ResultCodeParameter]).Value;
        switch (resultCode)
        {
            case SuccessCode:
                ticketLockName = (string)((IDbDataParameter)parameters[TicketLockNameParameter]).Value;
                markerTableName = (string)((IDbDataParameter)parameters[MarkerTableNameParameter]).Value;
                return true.AsValueTask();
            case FinishedPreambleWithoutAcquiringCode:
                ticketLockName = null;
                markerTableName = (string)((IDbDataParameter)parameters[MarkerTableNameParameter]).Value;
                return false.AsValueTask();
            case FailedToAcquireWithSpaceRemainingCode:
                throw new InvalidOperationException($"An internal semaphore algorithm error ({resultCode}) occurred: failed to acquire a ticket despite indication that tickets are available");
            case BusyWaitTimeoutCode:
                ticketLockName = markerTableName = null;
                return false.AsValueTask();
            case AllTicketsHeldByCurrentSessionCode:
                // whenever we hit this case, it's a deadlock. If the user asked us to wait forever, we just throw. However, 
                // if the user asked us to wait a specified amount of time we will wait in C#. There are other justifiable policies
                // but this one seems relatively safe and likely to do what you want. It seems reasonable that no one intends to hang
                // forever but also reasonable that someone should be able to test for lock acquisition without getting a throw
                if (timeout.IsInfinite)
                {
                    throw new DeadlockException("Deadlock detected: attempt to acquire the semaphore cannot succeed because all tickets are held by the current connection");
                }

                ticketLockName = markerTableName = null;

                async ValueTask<bool> DelayFalseAsync()
                {
                    await SyncViaAsync.Delay(timeout, cancellationToken).ConfigureAwait(false);
                    return false;
                }
                return DelayFalseAsync();
            case SqlApplicationLock.TimeoutExitCode:
                ticketLockName = markerTableName = null;
                return false.AsValueTask();
            default:
                ticketLockName = markerTableName = null;
                return FailAsync();

                async ValueTask<bool> FailAsync()
                {
                    if (resultCode < 0)
                    {
                        await SqlApplicationLock.ParseExitCodeAsync(resultCode, timeout, cancellationToken).ConfigureAwait(false);
                    }
                    throw new InvalidOperationException($"Unexpected semaphore algorithm result code {resultCode}");
                }
        }
    }

    private static DatabaseCommand CreateTextCommand(DatabaseConnection connection, TimeoutValue operationTimeout)
    {
        var command = connection.CreateCommand();
        command.SetTimeout(operationTimeout);
        return command;
    }

    private void AddCommonParameters(DatabaseCommand command, string semaphoreName, TimeoutValue? timeout = null, string? markerTableName = null, string? ticketLockName = null)
    {
        command.AddParameter(SemaphoreNameParameter, semaphoreName);
        command.AddParameter(MaxCountParameter, this.MaxCount);
        if (timeout.TryGetValue(out var timeoutValue))
        {
            command.AddParameter(TimeoutMillisParameter, timeoutValue.InMilliseconds);
        }

        command.AddParameter(ResultCodeParameter, type: DbType.Int32, direction: ParameterDirection.Output);

        var ticket = command.AddParameter(TicketLockNameParameter, ticketLockName, type: DbType.String);
        if (ticketLockName == null)
        {
            ticket.Direction = ParameterDirection.Output;
        }
        const int MaxOutputStringLength = 8000; // plenty long enough
        ticket.Size = MaxOutputStringLength;
        
        var markerTable = command.AddParameter(MarkerTableNameParameter, markerTableName, type: DbType.String);
        if (markerTableName == null)
        {
            markerTable.Direction = ParameterDirection.Output;
        }
        markerTable.Size = MaxOutputStringLength;
    }
    #endregion

    #region ---- Naming ----
    public static string ToSafeName(string semaphoreName)
    {
        // the max table name length is 128 for global and 116 for local temp tables. While we don't use local temp tables
        // currently, to be conservative we use the lower cap. We're using 115 not 116 to reflect the missing '#' which counts
        // towards the limit
        const int MaxTableNameLength = 115;
        const string Suffix = "semaphore";
        // this accounts for various other things we pad onto the name:
        // * Marker table adds SPID + "s" + WAITERNUMBER (10 + 1 + 10 = 21)
        // * Ticket lock name adds TICKETNUMBER (10)
        // * Intent table name adds "intent_" + SPID + "_" + TICKETNUMBER (7 + 10 + 1 + 10 = 28)
        // We will use 30 as a safe number
        const int AdditionalSuffixMaxLength = 30;

        var nameWithoutInvalidCharacters = ReplaceInvalidCharacters(semaphoreName);
        // note that we hash the original name, not the replaced name. This makes us even more robust to collisions
        var nameHash = HashName(semaphoreName);
        var maxBaseNameLength = MaxTableNameLength - (nameHash.Length + Suffix.Length + AdditionalSuffixMaxLength);
        var baseName = nameWithoutInvalidCharacters.Length <= maxBaseNameLength
            ? nameWithoutInvalidCharacters
            : nameWithoutInvalidCharacters.Substring(0, maxBaseNameLength);
        return $"{baseName}{nameHash}{Suffix}";
    }

    private static string ReplaceInvalidCharacters(string semaphoreName)
    {
        StringBuilder? modifiedName = null;
        for (var i = 0; i < semaphoreName.Length; ++i)
        {
            var @char = semaphoreName[i];
            if (!IsAsciiLetterOrDigit(@char))
            {
                if (modifiedName == null)
                {
                    modifiedName = new StringBuilder();
                    for (var j = 0; j < i; ++j) { modifiedName.Append(semaphoreName[j]); }
                }

                modifiedName.Append(((int)@char).ToString("x"));
            }
            else if (modifiedName != null)
            {
                modifiedName.Append(@char);
            }
        }

        return modifiedName?.ToString() ?? semaphoreName;
    }

    private static bool IsAsciiLetterOrDigit(char @char) => ('a' <= @char && @char <= 'z')
        || ('A' <= @char && @char <= 'Z')
        || ('0' <= @char && @char <= '9');

    private static string HashName(string name)
    {
        using var hashAlgorithm = SHA256.Create();
        var hashBytes = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(name));
        return BitConverter.ToString(hashBytes)
            .Replace("-", string.Empty)
            .ToLowerInvariant();
    }
    #endregion

    #region ---- Query Generation ----
    private const string SemaphoreNameParameter = "semaphoreName",
        MaxCountParameter = "maxCount",
        ResultCodeParameter = "resultCode",
        TimeoutMillisParameter = "timeoutMillis",
        MarkerTableNameParameter = "markerTableName",
        TicketLockNameParameter = "ticketLockName",
        LockResultVariable = "lockResult",
        LockScopeVariable = "lockScope",
        PreambleLockNameVariable = "preambleLock",
        BusyWaitLockNameVariable = "busyWaitLock";

    private const int SuccessCode = 0,
        FinishedPreambleWithoutAcquiringCode = 100,
        FailedToAcquireWithSpaceRemainingCode = 101,
        BusyWaitTimeoutCode = 102,
        AllTicketsHeldByCurrentSessionCode = SqlApplicationLock.AlreadyHeldExitCode;

    // when we don't have to deal with cancellation, we can put everything in one big query to save on round trips
    private static readonly Lazy<string> AcquireNonCancelableQuery = new Lazy<string>(() => Merge(
            CreateCommonVariableDeclarationsSql(includePreambleLock: true, includeBusyWaitLock: true, includeTryAcquireOnceVariables: true),
            CreateAcquirePreambleSql(willRetryInSeparateQueryAfterPreamble: null),
            CreateAcquireSql(cancelable: false),
            CreateCodaSql(includePreambleLockRelease: true, includeBusyWaitLockRelease: true)
        )),
        // for cancellation, we run the preamble first as non-cancellable followed by a cancelable busy wait. This 
        // ensures that we avoid the case where we create a marker table in the preamble and then cancel before returning it
        AcquireCancelablePreambleQuery = new Lazy<string>(() => Merge(
            CreateCommonVariableDeclarationsSql(includePreambleLock: true, includeBusyWaitLock: false, includeTryAcquireOnceVariables: true),
            CreateAcquirePreambleSql(willRetryInSeparateQueryAfterPreamble: true),
            CreateCodaSql(includePreambleLockRelease: true, includeBusyWaitLockRelease: false)
        )),
        AcquireCancelableQuery = new Lazy<string>(() => Merge(
            CreateCommonVariableDeclarationsSql(includePreambleLock: false, includeBusyWaitLock: true, includeTryAcquireOnceVariables: true),
            CreateAcquireSql(cancelable: true),
            CreateCodaSql(includePreambleLockRelease: false, includeBusyWaitLockRelease: true)
        )),
        CancellationCleanupQuery = new Lazy<string>(() => Merge(
            CreateCommonVariableDeclarationsSql(includePreambleLock: false, includeBusyWaitLock: true, includeTryAcquireOnceVariables: false),
            CreateCancellationCleanupSql(),
            CreateCodaSql(includePreambleLockRelease: false, includeBusyWaitLockRelease: true)
        )),
        ReleaseQuery = new Lazy<string>(() => Merge(
            CreateCommonVariableDeclarationsSql(includePreambleLock: false, includeBusyWaitLock: false, includeTryAcquireOnceVariables: false),
            CreateReleaseSql()
        ));

    private const string IntentMarkerTablePrefix = "intent";

    /// <summary>
    /// Used for making comments in format strings
    /// </summary>
    private static readonly object? C = null;
    
    /// <summary>
    /// The preamble is the first part of the acquire algorithm. It is not cancellation-safe
    /// </summary>
    private static string CreateAcquirePreambleSql(bool? willRetryInSeparateQueryAfterPreamble)
    {
        const string SpidCountSeparator = "s";
        // if everything is going smoothly then the preamble lock should never even come close to timing out since 
        // nothing blocking happens inside the preamble. However, to be safe we do eventually give up
        var preambleLockTimeoutMillis = (int)TimeSpan.FromMinutes(1).TotalMilliseconds;

        return $@"
                {C/* The preamble body executes inside a special lock. Since the preamble is designed to be
                 non-blocking we can wait for a long time on this lock without worrying about respecting
                 our timeout. We avoid waiting forever in case there are unexpected problems (e. g. a lock on sys.tables) */}
                EXEC @{LockResultVariable} = sys.sp_getapplock @{PreambleLockNameVariable}, 'Exclusive', @{LockScopeVariable}, {preambleLockTimeoutMillis}
                IF @{LockResultVariable} < 0 GOTO CODA

                {C/* First, we determine the number of existing waiters/holders so we know whether we will have to block or not.
                 At the same time, we determine a value for our marker table which has not been chosen yet. This value is 1 greater
                 than the greatest value that exists so far, so it can be > the count. The expression for determining this value is
                 somewhat complex. We are looking at table names like ##[sem name][spid][separator][value] and parsing out value. */}
                DECLARE @waiterNumber INT, @waiterCount INT
                SELECT TOP 1 @waiterNumber = ISNULL(MAX(CAST(SUBSTRING(name, CHARINDEX('{SpidCountSeparator}', name, LEN(@{SemaphoreNameParameter})) + 1, LEN(name)) AS INT) + 1), 0),
                    @waiterCount = COUNT(*)
                {C/* The NOLOCK here is important: otherwise we'll be blocked by trying to read entries for marker tables created in transactions that aren't committed */}
                FROM tempdb.sys.tables WITH(NOLOCK)
                {C/* Prefix search here is important since it uses an index. We don't need escaping because we bound the name to use a fixed character set */}
                WHERE name LIKE '##' + @{SemaphoreNameParameter} + '%'

                {C/* Create the marker table. This table is exists to give others a count of the number of waiting/holding processes.
                 we name our marker table using the form ##[sem name][spid][separator][value]. We use SPID over a random value since SPID values are typically small integers
                 that recycle over time; this means that we may be able to take advantage of SQL temp table caching. The reason we need SPID here at all is because if another
                 transaction creates and destroys a table of name X, we will be blocked if we try to create table X before the transaction ends. */}
                SET @{MarkerTableNameParameter} = '##' + @{SemaphoreNameParameter} + CAST(@@SPID AS NVARCHAR(MAX)) + '{SpidCountSeparator}' + CAST(@waiterNumber AS NVARCHAR(MAX))
                DECLARE @createMarkerTableSql NVARCHAR(MAX) = 'CREATE TABLE ' + @{MarkerTableNameParameter} + ' (_ BIT)'
                EXEC sp_executeSql @createMarkerTableSql

                {C/* If the number of waiters indicates that a space is free, we should be able to immediately acquire without blocking. */}
                IF @waiterCount < @{MaxCountParameter}
                BEGIN
                    {C/* may GOTO CODA; the CODA will release preamble lock */}
                    {CreateTryAcquireOnceSql(allowOneWait: false, cancelable: false)}

                    SET @{ResultCodeParameter} = {FailedToAcquireWithSpaceRemainingCode}
                    GOTO CODA {C/* the CODA will release preamble lock */}
                END
                                
                {C/* If we get here, it means we finished the preamble without acquiring a ticket */}
                {(
                // if this is the end of the query, we have to set an exit code. If we are going to retry we indicate the special code that will trigger that and otherwise we indicate
                // timeout. If this is not the end of the query, we just release the preamble lock and keep going
                willRetryInSeparateQueryAfterPreamble.TryGetValue(out var willRetryInSeparateQueryAfterPreambleValue)
                    ? $@"SET @{ResultCodeParameter} = {(willRetryInSeparateQueryAfterPreambleValue ? FinishedPreambleWithoutAcquiringCode : SqlApplicationLock.TimeoutExitCode)}"
                    : $"EXEC sys.sp_releaseapplock @{PreambleLockNameVariable}, @{LockScopeVariable}"
            )}";
    }

    private static string CreateAcquireSql(bool cancelable)
    {
        return $@" 
                {C/* The next step is to do a busy wait on all ticket locks. For fairness and to reduce resource usage, 
                 use a "busy wait lock" to permit only one thread to busy wait at a time. */}
                EXEC @{LockResultVariable} = sys.sp_getapplock @{BusyWaitLockNameVariable}, 'Exclusive', @{LockScopeVariable}, @{TimeoutMillisParameter}
                IF @{LockResultVariable} < 0 GOTO CODA

                DECLARE @expiry DATETIME2 = CASE WHEN @{TimeoutMillisParameter} < 0 THEN NULL ELSE DATEADD(ms, @{TimeoutMillisParameter}, SYSUTCDATETIME()) END
                WHILE 1 = 1
                BEGIN
                    {C/* may GOTO CODA; the CODA will release busy wait lock */}
                    {CreateTryAcquireOnceSql(allowOneWait: true, cancelable: cancelable)}

                    IF SYSUTCDATETIME() > @expiry
                    BEGIN
                        SET @{ResultCodeParameter} = {BusyWaitTimeoutCode}
                        GOTO CODA
                    END
                END";
    }

    private static string CreateCancellationCleanupSql()
    {
        // we check for the existence of an intent marker table since this indicates that we may have 
        // acquired a lock before being canceled and not have released it

        return $@"
                DECLARE @intentMarkerTableName NVARCHAR(MAX)
                SELECT TOP 1 @intentMarkerTableName = name
                FROM tempdb.sys.tables WITH(NOLOCK)
                WHERE name LIKE '##{IntentMarkerTablePrefix}\_' + CAST(@@SPID AS NVARCHAR(MAX)) + '\_%' ESCAPE '\'

                IF @intentMarkerTableName IS NOT NULL
                BEGIN
                    SET @{TicketLockNameParameter} = RIGHT(@intentMarkerTableName, LEN(@intentMarkerTableName) - LEN('##{IntentMarkerTablePrefix}_' + CAST(@@SPID AS NVARCHAR(MAX)) + '_'))
                    IF APPLOCK_MODE('public', @{TicketLockNameParameter}, @{LockScopeVariable}) != 'NoLock'
                        EXEC sys.sp_releaseapplock @{TicketLockNameParameter}, @{LockScopeVariable}

                    DECLARE @dropIntentMarkerTableSql NVARCHAR(MAX) = 'DROP TABLE ' + @intentMarkerTableName
                    EXEC sp_executeSql @dropIntentMarkerTableSql
                END
            ";
    }

    private static string CreateReleaseSql()
    {
        return $@"
                EXEC sys.sp_releaseAppLock @{TicketLockNameParameter}, @{LockScopeVariable}
                DECLARE @dropMarkerTableSql NVARCHAR(MAX) = 'DROP TABLE ' + @{MarkerTableNameParameter}
                EXEC sp_executeSql @dropMarkerTableSql";
    }

    private static string CreateCommonVariableDeclarationsSql(
        bool includePreambleLock, 
        bool includeBusyWaitLock,
        bool includeTryAcquireOnceVariables)
    {
        return $@"
                DECLARE @{LockResultVariable} INT
                    , @{LockScopeVariable} NVARCHAR(32) = CASE @@TRANCOUNT WHEN 0 THEN 'Session' ELSE 'Transaction' END
                {(includePreambleLock ? $", @{PreambleLockNameVariable} NVARCHAR(MAX) = 'preamble_' + @semaphoreName" : null)}
                {(includeBusyWaitLock ? $", @{BusyWaitLockNameVariable} NVARCHAR(MAX) = 'busyWait_' + @semaphoreName" : null)}
                {(includeTryAcquireOnceVariables ? @", @i INT, @baseTicketIndex INT, @anyNotHeld BIT" : null)}";
    }

    private static string CreateTryAcquireOnceSql(bool allowOneWait, bool cancelable)
    {
        return $@"
                SET @i = 0
                {C/* Rather than always looping through tickets 0 .. N-1, we start at a random ticket. This should reduce looping in the average case and means
                    that if we are allowing a wait then the one ticket we wait on is randomized. */}
                SET @baseTicketIndex = CAST(RAND() * @{MaxCountParameter} AS INT)
                SET @anyNotHeld = 0
                WHILE @i < @{MaxCountParameter}
                BEGIN
                    SET @{TicketLockNameParameter} = @{SemaphoreNameParameter} + CAST((@baseTicketIndex + @i) % @{MaxCountParameter} AS NVARCHAR(MAX))

                    {C/* Since app locks are reentrant on the same connection, we must do an explicit check to avoid taking the same ticket twice.
                     Additionally, if we are transaction-scoped we must check whether we hold the lock on EITHER the transaction or the session. */}
                    IF APPLOCK_MODE('public', @{TicketLockNameParameter}, @{LockScopeVariable}) = 'NoLock'
                        AND (@{LockScopeVariable} = 'Session' OR APPLOCK_MODE('public', @{TicketLockNameParameter}, 'Session') = 'NoLock')
                    BEGIN
                        {C/* "allowOneWait" will be specified when we are in a busy wait loop. To avoid burning CPU we pick the first unheld ticket we come
                         across and allow that wait to be 32ms instead of 0. This is preferable to doing WAITFOR since the wait will be broken if that ticket
                         becomes available. Note that we used to wait just 1ms here. However, in testing that proved flaky in detecting
                         deadlocks; empirically, 32ms seems to be sufficient to work reliably. The longer wait should also reduce the
                         CPU load without meaningfully adding delay overhead. */}
                        {(allowOneWait ? "DECLARE @lockTimeoutMillis INT = CASE @anyNotHeld WHEN 0 THEN 32 ELSE 0 END" : null)}
                        SET @anyNotHeld = 1

                        {(
                        cancelable
                            // The intent marker supports robust cancellation by ensuring that we never leave a lingering lock on a connection. By creating a marker before
                            // any lock acquisition, we have a way of determining the case where the query is canceled right after acquiring a lock
                            ? $@"DECLARE @intentMarkerTableName NVARCHAR(MAX) = '##{IntentMarkerTablePrefix}_' + CAST(@@SPID AS NVARCHAR(MAX)) + '_' + @{TicketLockNameParameter}
                                     DECLARE @createIntentMarkerTableSql NVARCHAR(MAX) = 'CREATE TABLE ' + @intentMarkerTableName + ' (_ BIT)'
                                     EXEC sp_executeSql @createIntentMarkerTableSql"
                            : null
                    )}

                        EXEC @{LockResultVariable} = sys.sp_getapplock @{TicketLockNameParameter}, 'Exclusive', @{LockScopeVariable}, {(allowOneWait ? "@lockTimeoutMillis" : "0")}
                        IF @{LockResultVariable} >= 0
                        BEGIN
                            SET @{ResultCodeParameter} = {SuccessCode}
                            GOTO CODA
                        END

                        {(
                        cancelable
                            // on any failed acquisition, drop the intent marker
                            ? $@"DECLARE @dropIntentMarkerTableSql NVARCHAR(MAX) = 'DROP TABLE ' + @intentMarkerTableName
                                     EXEC sp_executeSql @dropIntentMarkerTableSql"
                            : null
                    )}

                        {C/* on any unexpected lock failure, quit */}
                        IF @{LockResultVariable} < -1 GOTO CODA
                    END
                    SET @i = @i + 1
                END
                {C/* detect this as a special case since it means we'll never succeed. We can handle in C# */}
                IF @anyNotHeld = 0
                BEGIN
                    SET @{ResultCodeParameter} = {AllTicketsHeldByCurrentSessionCode}
                    GOTO CODA
                END
            ";
    }

    private static string CreateCodaSql(bool includePreambleLockRelease, bool includeBusyWaitLockRelease)
    {
        return $@"
                CODA:
                {(
                includePreambleLockRelease
                    ? $@"IF APPLOCK_MODE('public', @{PreambleLockNameVariable}, @{LockScopeVariable}) != 'NoLock'
                                EXEC sys.sp_releaseapplock @{PreambleLockNameVariable}, @{LockScopeVariable}"
                    : null
            )}
                {(
                includeBusyWaitLockRelease
                    ? $@"IF APPLOCK_MODE('public', @{BusyWaitLockNameVariable}, @{LockScopeVariable}) != 'NoLock'
                                EXEC sys.sp_releaseapplock @{BusyWaitLockNameVariable}, @{LockScopeVariable}"
                    : null
            )}
                IF @{ResultCodeParameter} IS NULL AND @{LockResultVariable} < 0
                    SET @{ResultCodeParameter} = @{LockResultVariable}
                IF @{ResultCodeParameter} NOT IN ({SuccessCode}, {FinishedPreambleWithoutAcquiringCode})
                BEGIN
                    IF OBJECT_ID('tempdb..' + @{MarkerTableNameParameter}) IS NOT NULL
                    BEGIN
                        DECLARE @dropMarkerTableSql NVARCHAR(MAX) = 'DROP TABLE ' + @{MarkerTableNameParameter}
                        EXEC sp_executeSql @dropMarkerTableSql
                    END
                END";
    }

    private static string Merge(params string[] parts) => string.Join(Environment.NewLine, parts);
    #endregion
}
