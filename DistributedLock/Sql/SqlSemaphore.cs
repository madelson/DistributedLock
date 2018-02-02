using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    // todo NOLOCK everywhere?
    // todo NOLOCK vs?
    // todo TOP 1 for NOLOCK queries?
    // todo "deadlock" handling => throw for inf timeout

    internal sealed class SqlSemaphore : ISqlSynchronizationStrategy<SqlSemaphore.Cookie>
    {
        private readonly int _maxCount;

        public SqlSemaphore(int maxCount)
        {
            this._maxCount = maxCount;
        }

        #region ---- Execution ----
        public Cookie TryAcquire(ConnectionOrTransaction connectionOrTransaction, string resourceName, int timeoutMillis)
        {
            var tryAcquireTask = this.TryAcquireAsync(connectionOrTransaction, resourceName, timeoutMillis, CancellationToken.None, isSyncOverAsync: true);
            try { return tryAcquireTask.Result; }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.GetBaseException()).Throw();
                throw; // never hit
            }
        }

        public Task<Cookie> TryAcquireAsync(ConnectionOrTransaction connectionOrTransaction, string resourceName, int timeoutMillis, CancellationToken cancellationToken)
        {
            return this.TryAcquireAsync(connectionOrTransaction, resourceName, timeoutMillis, cancellationToken, isSyncOverAsync: false);
        }

        public void Release(ConnectionOrTransaction connectionOrTransaction, string resourceName, Cookie lockCookie)
        {
            using (var command = CreateTextCommand(connectionOrTransaction, operationTimeoutMillis: Timeout.Infinite))
            {
                command.CommandText = ReleaseQuery.Value;
                this.AddCommonParameters(command, resourceName, markerTableName: lockCookie.MarkerTable, ticketLockName: lockCookie.Ticket);
                command.ExecuteNonQuery();
            }
        }

        private static Cookie ProcessAcquireResult(
            IDbDataParameter resultCode,
            IDbDataParameter ticket,
            IDbDataParameter markerTableName)
        {
            switch ((int)resultCode.Value)
            {
                case SuccessCode:
                    return new Cookie(ticket: (string)ticket.Value, markerTable: (string)markerTableName.Value);
                case BusyWaitTimeoutCode:
                    return null;
                case FailedToAcquireWithSpaceRemainingCode:
                    // todo better error message
                    throw new InvalidOperationException(nameof(FailedToAcquireWithSpaceRemainingCode));
                default:
                    // todo
                    throw new NotImplementedException(resultCode.Value.ToString());
            }
        }

        bool ISqlSynchronizationStrategy<Cookie>.IsUpgradeable => false;

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
        private async Task<Cookie> TryAcquireAsync(
            ConnectionOrTransaction connectionOrTransaction, 
            string semaphoreName, 
            int timeoutMillis, 
            CancellationToken cancellationToken, 
            bool isSyncOverAsync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string markerTableName;  

            // for the special cases where we are not supporting cancellation or where the timeout is zero,
            // we can use abbreviated single-query algorithms
            if (timeoutMillis == 0 || !cancellationToken.CanBeCanceled)
            {
                using (var command = CreateTextCommand(connectionOrTransaction, operationTimeoutMillis: timeoutMillis))
                {
                    command.CommandText = (timeoutMillis == 0 ? AcquireZeroQuery : AcquireNonCancelableQuery).Value;
                    this.AddCommonParameters(command, semaphoreName, timeoutMillis: timeoutMillis == 0 ? default(int?) : timeoutMillis);
                    await ExecuteNonQueryAsync(command, CancellationToken.None, isSyncOverAsync).ConfigureAwait(false);
                    return ProcessAcquireResult(command.Parameters, out markerTableName, out var ticketLockName)
                        ? new Cookie(ticket: ticketLockName, markerTable: markerTableName)
                        : null;
                }
            }

            // cancelable case

            using (var command = CreateTextCommand(connectionOrTransaction, operationTimeoutMillis: timeoutMillis))
            {
                command.CommandText = AcquireCancelablePreambleQuery.Value;
                this.AddCommonParameters(command, semaphoreName);
                // preamble is non-cancelable
                await ExecuteNonQueryAsync(command, CancellationToken.None, isSyncOverAsync).ConfigureAwait(false);
                if (ProcessAcquireResult(command.Parameters, out markerTableName, out var ticketLockName))
                {
                    return new Cookie(ticket: ticketLockName, markerTable: markerTableName);
                }
            }

            using (var command = CreateTextCommand(connectionOrTransaction, operationTimeoutMillis: timeoutMillis))
            {
                command.CommandText = AcquireCancelableQuery.Value;
                this.AddCommonParameters(command, semaphoreName, timeoutMillis: timeoutMillis, markerTableName: markerTableName);
                try
                {
                    await ExecuteNonQueryAsync(command, cancellationToken, isSyncOverAsync).ConfigureAwait(false);
                }
                catch when (cancellationToken.IsCancellationRequested)
                {
                    // if we canceled the query, we need to perform cleanup to make sure we don't leave marker tables or held locks

                    using (var cleanupCommand = CreateTextCommand(connectionOrTransaction, operationTimeoutMillis: 0))
                    {
                        cleanupCommand.CommandText = CancellationCleanupQuery.Value;
                        this.AddCommonParameters(cleanupCommand, semaphoreName, markerTableName: markerTableName);
                        await ExecuteNonQueryAsync(cleanupCommand, CancellationToken.None, isSyncOverAsync).ConfigureAwait(false);
                    }
                    
                    throw;
                }

                return ProcessAcquireResult(command.Parameters, out markerTableName, out var ticketLockName)
                    ? new Cookie(ticket: ticketLockName, markerTable: markerTableName)
                    : null;
            }
        }

        private static bool ProcessAcquireResult(IDataParameterCollection parameters, out string markerTableName, out string ticketLockName)
        {
            var resultCode = (int)((IDbDataParameter)parameters[ResultCodeParameter]).Value;
            switch (resultCode)
            {
                case SuccessCode:
                    ticketLockName = (string)((IDbDataParameter)parameters[TicketLockNameParameter]).Value;
                    markerTableName = (string)((IDbDataParameter)parameters[MarkerTableNameParameter]).Value;
                    return true;
                case FinishedPreambleWithoutAcquiringCode:
                    ticketLockName = null;
                    markerTableName = (string)((IDbDataParameter)parameters[MarkerTableNameParameter]).Value;
                    return false;
                case FailedToAcquireWithSpaceRemainingCode:
                    throw new InvalidOperationException($"An internal semaphore algorithm error ({resultCode}) occurred: failed to acquire a ticket despite indication that tickets are available");
                case BusyWaitTimeoutCode:
                    ticketLockName = markerTableName = null;
                    return false;
                case AllTicketsHeldByCurrentSessionCode:
                    throw new NotImplementedException("todo");
                case SqlApplicationLock.TimeoutExitCode:
                    ticketLockName = markerTableName = null;
                    return false;
                default:
                    if (resultCode < 0)
                    {
                        SqlApplicationLock.ParseExitCode(resultCode);
                    }
                    throw new InvalidOperationException($"Unexpected semaphore algorithm result code {resultCode}");
            }
        }
        
        private static async Task ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken, bool isSyncOverAsync)
        {
            if (isSyncOverAsync)
            {
                command.ExecuteNonQuery();
                return;
            }

            // note: we can't call ExecuteNonQueryAsync(cancellationToken) here because of
            // what appears to be a .NET bug (see https://github.com/dotnet/corefx/issues/26623,
            // https://stackoverflow.com/questions/48461567/canceling-query-with-while-loop-hangs-forever)
            // The workaround is to fall back to multi-threaded async querying in the case where we
            // have a live cancellation token (less efficient but at least it works)

            if (!cancellationToken.CanBeCanceled)
            {
                await command.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
                return;
            }
            
            var commandTask = Task.Run(() => command.ExecuteNonQuery());
            using (cancellationToken.Register(() =>
            {
                // we call cancel in a loop here until the command task completes. This is because
                // when cancellation triggers it's possible the task hasn't even run yet. Therefore
                // we want to keep trying until we know cancellation has worked
                var spinWait = new SpinWait();
                while (true)
                {
                    try { command.Cancel(); }
                    catch { /* just ignore errors here */ }

                    if (commandTask.IsCompleted) { break; }
                    spinWait.SpinOnce();
                }
            }))
            {
                try { await commandTask.ConfigureAwait(false); }
                catch (SqlException ex)
                    // MA: canceled SQL operations throw SqlException instead of OCE.
                    // That means that downstream operations end up faulted instead of canceled. We
                    // wrap with OCE here to correctly propagate cancellation
                    when (cancellationToken.IsCancellationRequested && ex.Number == 0)
                {
                    throw new OperationCanceledException("Command was canceled", ex, cancellationToken);
                }
            }
        }

        private static IDbCommand CreateTextCommand(ConnectionOrTransaction connectionOrTransaction, int operationTimeoutMillis)
        {
            var command = connectionOrTransaction.Connection.CreateCommand();
            command.Transaction = connectionOrTransaction.Transaction;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = SqlHelpers.GetCommandTimeout(operationTimeoutMillis);
            return command;
        }

        private void AddCommonParameters(IDbCommand command, string semaphoreName, int? timeoutMillis = null, string markerTableName = null, string ticketLockName = null)
        {
            command.Parameters.Add(command.CreateParameter(SemaphoreNameParameter, semaphoreName));
            command.Parameters.Add(command.CreateParameter(MaxCountParameter, this._maxCount));
            if (timeoutMillis.HasValue)
            {
                command.Parameters.Add(command.CreateParameter(TimeoutMillisParameter, timeoutMillis));
            }

            var resultCode = command.CreateParameter(ResultCodeParameter, null);
            resultCode.DbType = DbType.Int32;
            resultCode.Direction = ParameterDirection.Output;
            command.Parameters.Add(resultCode);

            var ticket = command.CreateParameter(TicketLockNameParameter, ticketLockName);
            if (ticketLockName == null)
            {
                ticket.Direction = ParameterDirection.Output;
            }
            ticket.DbType = DbType.String;
            const int MaxOutputStringLength = 8000; // plenty long enough
            ticket.Size = MaxOutputStringLength;
            command.Parameters.Add(ticket);

            var markerTable = command.CreateParameter(MarkerTableNameParameter, markerTableName);
            if (markerTableName == null)
            {
                markerTable.Direction = ParameterDirection.Output;
            }
            markerTable.DbType = DbType.String;
            markerTable.Size = MaxOutputStringLength;
            command.Parameters.Add(markerTable);
        }
        #endregion

        #region ---- Naming ----
        public static string ToSafeName(string semaphoreName)
        {
            // todo readjust name limit to leave a larger buffer for stuff

            // the max table name length is 128 for global and 116 for local temp tables. While we don't use local temp tables
            // currently, to be conservative we use the lower cap. We're using 115 not 116 to reflect the missing '#' which counts
            // towards the limit
            const int MaxTableNameLength = 115;
            const string Suffix = "semaphore";
            // technically this is long.MaxValue.ToString().Length. I'm using long vs. int to give future flexibility
            const int IntegerSuffixMaxLength = 19;

            var nameWithoutInvalidCharacters = ReplaceInvalidCharacters(semaphoreName);
            // note that we hash the original name, not the replaced name. This makes us even more robust to collisions
            var nameHash = HashName(semaphoreName);
            var maxBaseNameLength = MaxTableNameLength - (nameHash.Length + Suffix.Length + IntegerSuffixMaxLength);
            var baseName = nameWithoutInvalidCharacters.Length <= maxBaseNameLength
                ? nameWithoutInvalidCharacters
                : nameWithoutInvalidCharacters.Substring(0, maxBaseNameLength);
            return $"{baseName}{nameHash}{Suffix}";
        }

        private static string ReplaceInvalidCharacters(string semaphoreName)
        {
            StringBuilder modifiedName = null;
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
            using (var hashAlgorithm = SHA256.Create())
            {
                var hashBytes = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(name));
                return BitConverter.ToString(hashBytes)
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
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
            AllTicketsHeldByCurrentSessionCode = 103;

        // acquire with zero timeout just needs to run the preamble since past the preamble blocking is required
        private static readonly Lazy<string> AcquireZeroQuery = new Lazy<string>(() => Merge(
                CreateCommonVariableDeclarationsSql(includePreambleLock: true, includeBusyWaitLock: false, includeTryAcquireOnceVariables: true),
                CreateAcquirePreambleSql(willRetryInSeparateQueryAfterPreamble: false),
                CreateCodaSql(includePreambleLockRelease: true, includeBusyWaitLockRelease: false)
            )),
            // when we don't have to deal with cancellation, we can put everything in one big query to save on round trips
            AcquireNonCancelableQuery = new Lazy<string>(() => Merge(
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
        private static readonly object C = null;
        
        /// <summary>
        /// The preamble is the first part of the acquire algorithm. It is not cancellation-safe
        /// </summary>
        private static string CreateAcquirePreambleSql(bool? willRetryInSeparateQueryAfterPreamble)
        {
            // todo factor this into the length calcs
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
                SELECT @waiterNumber = ISNULL(MAX(CAST(SUBSTRING(name, CHARINDEX('{SpidCountSeparator}', name, LEN(@{SemaphoreNameParameter})) + 1, LEN(name)) AS INT) + 1), 0),
                    @waiterCount = COUNT(*)
                {C/* The NOLOCK here is important: otherwise we'll be blocked by trying to read entries for marker tables created in transactions that aren't committed */}
                FROM (SELECT * FROM tempdb.sys.tables WITH(NOLOCK)) x
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
                    willRetryInSeparateQueryAfterPreamble.HasValue
                        ? $@"SET @{ResultCodeParameter} = {(willRetryInSeparateQueryAfterPreamble.Value ? FinishedPreambleWithoutAcquiringCode : SqlApplicationLock.TimeoutExitCode)}"
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

        // todo test where some tickets are taken on connection and then others are taken on transaction
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

                    {C/* Since app locks are reentrant on the same connection, we must do an explicit check to avoid taking the same ticket twice */}
                    IF APPLOCK_MODE('public', @{TicketLockNameParameter}, @{LockScopeVariable}) = 'NoLock'
                    BEGIN
                        {C/* "allowOneWait" will be specified when we are in a busy wait loop. To avoid burning CPU we pick the first unheld ticket we come
                             across and allow that wait to be 1ms instead of 0. This is preferable to doing WAITFOR since the wait will be broken if that ticket
                             becomes available. */}
                        {(allowOneWait ? "DECLARE @lockTimeoutMillis INT = CASE @anyNotHeld WHEN 0 THEN 1 ELSE 0 END" : null)}
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
}
