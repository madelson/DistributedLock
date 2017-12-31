using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    // potentially simpler approach:
    // take init lock
    // count tickets and get max ticket id
    // if (count < maxCount)
    //      take hold lock (loop through each with no wait. We MUST find one since there is space; return error if not)
    //      release init lock
    //      return taken hold lock
    // else
    //      myTicket = maxTicketId + 1
    //      create table ##myTicket
    //      release init lock
    // if (try take spinWaitLock (we know there's contention if we reach here so if we time out it's over))
    //      spin to take hold lock until timeout
    //      if success, set taken hold lock
    //      release spinWaitLock
    // finally 
    //      return takenHoldLock, ticket
    //
    // to release:
    //      release takenHoldLock
    //      drop table ticket

    // approach
    // todo need to do something with checking for number of holders + waiters here
    // take quick-lock
    // find ## tables of the form name_{count} and store them in a # table
    // create your own marker ## table and take wait_lock for it
    // release quick-lock
    // take each wait-lock in # table ordered by desc
    // spin taking hold-locks
    // finally
    // release wait-locks
    // delete ## table

    /* IDEA
     
DECLARE @initializationLockName NVARCHAR(255) = 'abc'
DECLARE @baseWaitLockName NVARCHAR(MAX) = 'abc_wait'
DECLARE @baseWaitLockNameChars INT = LEN(@baseWaitLockName)
DECLARE @baseHoldLockName NVARCHAR(255) = 'abc_hold'
DECLARE @maxCount INT = 3
DECLARE @lockResult INT;

CREATE TABLE #distributedSemaphoreHelper (id INT PRIMARY KEY, waitLockName NVARCHAR(255))

EXEC @lockResult = sys.sp_getapplock @initializationLockName, 'Exclusive', 'Session', -1
IF @lockResult < 0 GOTO CODA

INSERT INTO #distributedSemaphoreHelper (id, waitLockName)
SELECT CAST(SUBSTRING(name, @baseWaitLockNameChars, 1000) AS INT) AS id, name AS waitLockName
FROM sys.tables
WHERE name LIKE @baseWaitLockName + '%[0-9]'

DECLARE @waitLockName NVARCHAR(255) = @baseWaitLockName + (SELECT CAST(MAX(id) + 1 AS NVARCHAR(MAX)) FROM #distributedSemaphoreHelper)
EXEC('CREATE TABLE ' + @waitLockName + ' (_ BIT)');
EXEC @lockResult = sys.sp_getapplock @waitLockName, 'Exclusive', 'Session', -1
IF @lockResult < 0 GOTO CODA

EXEC sys.sp_releaseapplock @initializationLockName, 'Session'

DECLARE @nextWaitLock NVARCHAR(255)
DECLARE waitLocks CURSOR LOCAL FAST_FORWARD FOR
	SELECT waitLockName FROM #distributedSemaphoreHelper ORDER BY id DESC

OPEN waitLocks
FETCH waitLocks INTO @nextWaitLock
WHILE @@FETCH_STATUS = 0
BEGIN
	EXEC @lockResult = sys.sp_getapplock @nextWaitLock, 'Exclusive', 'Session', -1 -- todo timeout
	IF @lockResult < 0 GOTO CODA
	FETCH waitLocks INTO @nextWaitLock
END

WHILE 1 = 1
BEGIN
	DECLARE @i INT = 0
	WHILE @i < @maxCount
	BEGIN
		DECLARE @holdLockName NVARCHAR(255) = @baseHoldLockName + CAST(@i AS NVARCHAR(MAX))
		EXEC @lockResult = sys.sp_getapplock @holdLockName, 'Exclusive', 'Session'
		IF @lockResult >= 0 GOTO CODA -- todo probably set success = true here
	END

	WAITFOR DELAY '00:00:00.005'
END

CODA:
-- release all wait locks
-- drop all temp tables
-- return selected hold lock so we can release it later


    */

    internal sealed class SemaphoreHelper
    {
        private static readonly string AcquireSql = CreateAcquireQuery(),
            ReleaseSql = CreateReleaseQuery();

        private readonly string _safeName;
        private readonly int _maxCount;

        public SemaphoreHelper(string semaphoreName, int maxCount)
        {
            this._safeName = ToSafeName(semaphoreName);
            this._maxCount = maxCount;
        }

        #region ---- Execution ----
        public IDisposable TryAcquire(
            ConnectionOrTransaction connectionOrTransaction,
            int timeoutMillis,
            CancellationToken cancellationToken)
        {
            using (var command = this.CreateAcquireCommand(
                connectionOrTransaction,
                timeoutMillis,
                resultCode: out var resultCode,
                ticket: out var ticket,
                markerTable: out var markerTable))
            {
                command.ExecuteNonQueryAsync(cancellationToken);
                return this.ProcessAcquireResult(connectionOrTransaction, resultCode: resultCode, ticket: ticket, markerTableName: markerTable);
            }
        }

        public async Task<IDisposable> TryAcquireAsync(
            ConnectionOrTransaction connectionOrTransaction, 
            int timeoutMillis, 
            CancellationToken cancellationToken)
        {
            using (var command = this.CreateAcquireCommand(
                connectionOrTransaction, 
                timeoutMillis, 
                resultCode: out var resultCode, 
                ticket: out var ticket, 
                markerTable: out var markerTable))
            {
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                return this.ProcessAcquireResult(connectionOrTransaction, resultCode: resultCode, ticket: ticket, markerTableName: markerTable);
            }
        }

        private LockHandle ProcessAcquireResult(
            ConnectionOrTransaction connectionOrTransaction, 
            IDbDataParameter resultCode,
            IDbDataParameter ticket,
            IDbDataParameter markerTableName)
        {
            switch ((int)resultCode.Value)
            {
                case 0:
                    return new LockHandle(this, connectionOrTransaction, (string)ticket.Value, (string)markerTableName.Value);
                case LockTimeout:
                    return null;
                default:
                    throw new NotImplementedException(resultCode.Value.ToString());
            }
        }

        private sealed class LockHandle : IDisposable
        {
            private SemaphoreHelper _helper;
            private ConnectionOrTransaction _connectionOrTransaction;
            private string _ticket, _markerTable;

            public LockHandle(
                SemaphoreHelper helper, 
                ConnectionOrTransaction connectionOrTransaction, 
                string ticket, 
                string markerTable)
            {
                this._helper = helper;
                this._connectionOrTransaction = connectionOrTransaction;
                this._ticket = ticket;
                this._markerTable = markerTable;
            }

            public void Dispose()
            {
                var helper = Interlocked.Exchange(ref this._helper, null);
                if (helper == null) { return; }

                using (var command = helper.CreateReleaseCommand(this._connectionOrTransaction, ticket: this._ticket, markerTable: this._markerTable))
                {
                    command.ExecuteNonQuery();
                }

                this._connectionOrTransaction = default(ConnectionOrTransaction);
                this._ticket = this._markerTable = null;
            }
        }
        #endregion

        #region ---- Command Creation ----
        private IDbCommand CreateAcquireCommand(
            ConnectionOrTransaction connectionOrTransaction, 
            int timeoutMillis,
            out IDbDataParameter resultCode,
            out IDbDataParameter ticket,
            out IDbDataParameter markerTable)
        {
            var command = connectionOrTransaction.Connection.CreateCommand();
            command.Transaction = connectionOrTransaction.Transaction;
            command.CommandText = AcquireSql;
            command.CommandType = CommandType.Text;
            // todo share code
            command.CommandTimeout = timeoutMillis >= 0
              // command timeout is in seconds. We always wait at least the lock timeout plus a buffer 
              ? (timeoutMillis / 1000) + 30
              // otherwise timeout is infinite so we use the infinite timeout of 0
              // (see https://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqlcommand.commandtimeout%28v=vs.110%29.aspx)
              : 0;

            command.Parameters.Add(command.CreateParameter(SemaphoreNameParameter, this._safeName));
            command.Parameters.Add(command.CreateParameter(MaxCountParameter, this._maxCount));
            command.Parameters.Add(command.CreateParameter(TimeoutMillisParameter, timeoutMillis));

            resultCode = command.CreateParameter(ResultCodeParameter, null);
            resultCode.DbType = DbType.Int32;
            resultCode.Direction = ParameterDirection.Output;
            command.Parameters.Add(resultCode);

            ticket = command.CreateParameter(TicketLockNameParameter, null);
            ticket.Direction = ParameterDirection.Output;
            ticket.DbType = DbType.String;
            const int MaxOutputStringLength = 8000; // plenty long enough
            ticket.Size = MaxOutputStringLength;
            command.Parameters.Add(ticket);

            markerTable = command.CreateParameter(MarkerTableNameParameter, null);
            markerTable.Direction = ParameterDirection.Output;
            markerTable.DbType = DbType.String;
            markerTable.Size = MaxOutputStringLength; 
            command.Parameters.Add(markerTable);

            return command;
        }

        private IDbCommand CreateReleaseCommand(ConnectionOrTransaction connectionOrTransaction, string ticket, string markerTable)
        {
            var command = connectionOrTransaction.Connection.CreateCommand();
            command.Transaction = connectionOrTransaction.Transaction;
            command.CommandType = CommandType.Text;
            command.CommandText = ReleaseSql;

            command.Parameters.Add(command.CreateParameter(TicketLockNameParameter, ticket));
            command.Parameters.Add(command.CreateParameter(MarkerTableNameParameter, markerTable));

            return command;
        }
        #endregion

        // TODO everything should be case-sensitive here to match sp_getapplock's behavior (we should add a common test for this)
        #region ---- Naming ----
        private static string ToSafeName(string semaphoreName)
        {
            // note: this is documented as being 128, but for temp tables it's actually 116
            const int MaxTableNameLength = 116;
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
                if (!('a' <= @char && @char <= 'z'))
                {
                    if (modifiedName == null)
                    {
                        modifiedName = new StringBuilder();
                        for (var j = 0; j < i; ++j) { modifiedName.Append(semaphoreName[j]); }
                    }

                    if ('A' <= @char && @char <= 'Z')
                    {
                        modifiedName.Append(char.ToLowerInvariant(@char));
                    }
                    else
                    {
                        modifiedName.Append(((int)@char).ToString("x"));
                    }
                }
                else if (modifiedName != null)
                {
                    modifiedName.Append(@char);
                }
            }

            return modifiedName?.ToString() ?? semaphoreName;
        }

        private static string HashName(string name)
        {
            using (var md5 = MD5.Create())
            {
                var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(name.ToUpperInvariant()));
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
            TicketLockNameParameter = "ticketLockName";

        private const int Success = 0,
            // TODO could instead make this positive and just use lockresult for the others...
            FailedToAcquireWithSpaceRemaining = -1,
            LockTimeout = -2,
            LockDeadlock = -3,
            LockCancel = -4,
            GenericLockError = -5;

        private static string CreateAcquireQuery()
        {
            // todo figure out cancellation story. The problem is that canceling after creating the table
            // or taking one of the locks leaks the cancel. To address, we can instead run the first query with
            // no cancellation support select out the marker table as we get it them so that we can clean it up later.
            // Still need to figure out the best approach for the busy wait (e. g. manually running cleanup post-cancel)

            string GetSpinWaitSql(bool checkExpiry)
            {
                // todo handle reentrance here; we don't want to take the same lock twice!
                // todo instead of always looping from 0 and waiting on count-1, we should loop from rand [0, count)
                // and just wait on the countth lock. This is important for reentrance since if you own the last lock
                // then you'll spin without sleeping if we skip it
                return $@"                    
                    SET @i = 0
                    WHILE @i < @{MaxCountParameter}
                    BEGIN
                        SET @{TicketLockNameParameter} = @{SemaphoreNameParameter} + CAST(@i AS NVARCHAR(MAX))
                        {(
                            checkExpiry
                                ? @"SET @lockTimeoutMillis = 5
                                    IF DATEADD(ms, @lockTimeoutMillis, SYSUTCDATETIME()) > @expiry SET @lockTimeoutMillis = 1"
                                : string.Empty
                        )}
                        EXEC @lockResult = sys.sp_getapplock @{TicketLockNameParameter}, 'Exclusive', 'Session', {(checkExpiry ? "@lockTimeoutMillis" : "0")}
                        IF @lockResult >= 0
                        BEGIN
                            SET @{ResultCodeParameter} = {Success}
                            GOTO CODA
                        END
                        IF @lockResult < -1 GOTO CODA
                        SET @i = @i + 1
                    END";
            }

            return $@"
                DECLARE @lockResult INT
                DECLARE @initializationLock NVARCHAR(MAX) = @{SemaphoreNameParameter} + '_init'
                DECLARE @waiterNumber INT
                DECLARE @waiterCount INT
                DECLARE @i INT
                DECLARE @markerTableSql NVARCHAR(MAX)
                DECLARE @expiry DATETIME2
                DECLARE @lockTimeoutMillis INT
                DECLARE @busyWaitLock NVARCHAR(MAX) = @{SemaphoreNameParameter} + '_busyWait'

                EXEC @lockResult = sys.sp_getapplock @initializationLock, 'Exclusive', 'Session', -1
                IF @lockResult < 0 GOTO CODA

                SELECT @waiterNumber = ISNULL(MAX(CAST(RIGHT(name, (LEN(name) - (LEN(@{SemaphoreNameParameter}) + 2))) AS INT) + 1), 0),
                    @waiterCount = COUNT(*)
                FROM tempdb.sys.tables WITH(NOLOCK)
                WHERE name LIKE '##' + REPLACE(REPLACE(REPLACE(@{SemaphoreNameParameter}, '\', '\\'), '_', '\_'), '%', '\%') + '%' ESCAPE '\'

                SET @{MarkerTableNameParameter} = 'tempdb..##' + @{SemaphoreNameParameter} + CAST(@waiterNumber AS NVARCHAR(MAX))
                SET @markerTableSql = 'CREATE TABLE ' + @{MarkerTableNameParameter} + ' (_ BIT)'
                EXEC sp_executeSql @markerTableSql

                IF @waiterCount < @{MaxCountParameter}
                BEGIN
                    {GetSpinWaitSql(checkExpiry: false)}

                    SET @{ResultCodeParameter} = {FailedToAcquireWithSpaceRemaining}
                    GOTO CODA
                END
                
                EXEC sys.sp_releaseapplock @initializationLock, 'Session'

                SET @expiry = CASE WHEN @{TimeoutMillisParameter} < 0 THEN NULL ELSE DATEADD(ms, @{TimeoutMillisParameter}, SYSUTCDATETIME()) END
                
                EXEC @lockResult = sys.sp_getapplock @busyWaitLock, 'Exclusive', 'Session', @{TimeoutMillisParameter}
                IF @lockResult < 0 GOTO CODA

                WHILE 1 = 1
                BEGIN
                    {GetSpinWaitSql(checkExpiry: true)}

                    IF SYSUTCDATETIME() > @expiry GOTO CODA
                END

                CODA:
                IF APPLOCK_MODE('public', @initializationLock, 'Session') != 'NoLock'
                    EXEC sys.sp_releaseapplock @initializationLock, 'Session'
                IF APPLOCK_MODE('public', @busyWaitLock, 'Session') != 'NoLock'
                    EXEC sys.sp_releaseapplock @busyWaitLock, 'Session'
                IF @{ResultCodeParameter} IS NULL
                    SET @{ResultCodeParameter} = CASE @lockResult 
                        WHEN -1 THEN {LockTimeout}
                        WHEN -2 THEN {LockCancel}
                        WHEN -3 THEN {LockDeadlock}
                        ELSE {GenericLockError}
                        END
                IF @{ResultCodeParameter} != {Success}
                BEGIN
                    IF OBJECT_ID(@{MarkerTableNameParameter}) IS NOT NULL
                    BEGIN
                        SET @markerTableSql = 'DROP TABLE ' + @{MarkerTableNameParameter}
                        EXEC sp_executeSql @markerTableSql
                    END
                END
            ";
        }

        private static string CreateReleaseQuery()
        {
            return $@"
                EXEC sp_releaseAppLock @{TicketLockNameParameter}, 'Session'
                DECLARE @markerTableSql NVARCHAR(MAX) = 'DROP TABLE ' + @{MarkerTableNameParameter}
                EXEC sp_executeSql @markerTableSql";
        }
        #endregion
    }
}
