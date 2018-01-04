using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Sql
{
    internal sealed class SqlSemaphore : ISqlSynchronizationStrategy<SqlSemaphore.Cookie>
    {
        private static readonly string AcquireSql = CreateAcquireQuery(),
            ReleaseSql = CreateReleaseQuery();
        
        private readonly int _maxCount;

        public SqlSemaphore(int maxCount)
        {
            this._maxCount = maxCount;
        }

        #region ---- Execution ----
        public Cookie TryAcquire(ConnectionOrTransaction connectionOrTransaction, string resourceName, int timeoutMillis)
        {
            using (var command = this.CreateAcquireCommand(
                connectionOrTransaction,
                resourceName,
                timeoutMillis,
                resultCode: out var resultCode,
                ticket: out var ticket,
                markerTable: out var markerTable))
            {
                // todo on timeout this should release the init lock. Alternatively, we could give the init lock a long but lesser
                // timeout so that that would not happen
                command.ExecuteNonQuery();
                return ProcessAcquireResult(resultCode: resultCode, ticket: ticket, markerTableName: markerTable);
            }
        }

        public async Task<Cookie> TryAcquireAsync(ConnectionOrTransaction connectionOrTransaction, string resourceName, int timeoutMillis, CancellationToken cancellationToken)
        {
            using (var command = this.CreateAcquireCommand(
                connectionOrTransaction,
                resourceName,
                timeoutMillis,
                resultCode: out var resultCode,
                ticket: out var ticket,
                markerTable: out var markerTable))
            {
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                return ProcessAcquireResult(resultCode: resultCode, ticket: ticket, markerTableName: markerTable);
            }
        }

        public void Release(ConnectionOrTransaction connectionOrTransaction, string resourceName, Cookie lockCookie)
        {
            using (var command = this.CreateReleaseCommand(connectionOrTransaction, ticket: lockCookie.Ticket, markerTable: lockCookie.MarkerTable))
            {
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
                case 0:
                    return new Cookie(ticket: (string)ticket.Value, markerTable: (string)markerTableName.Value);
                case LockTimeout:
                    return null;
                case FailedToAcquireWithSpaceRemaining:
                    // todo better error message
                    throw new InvalidOperationException(nameof(FailedToAcquireWithSpaceRemaining));
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
                this.Ticket = ticket;
                this.MarkerTable = markerTable;
            }

            public string Ticket { get; }
            public string MarkerTable { get; }
        }

        private sealed class LockHandle : IDisposable
        {
            private SqlSemaphore _helper;
            private ConnectionOrTransaction _connectionOrTransaction;
            private string _ticket, _markerTable;

            public LockHandle(
                SqlSemaphore helper, 
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
            string semaphoreName,
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

            command.Parameters.Add(command.CreateParameter(SemaphoreNameParameter, semaphoreName));
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

        // TODO everything should be case-SENSITIVE here to match sp_getapplock's behavior (we should add a common test for this)
        // TODO change to SHA2; it's faster and more secure
        #region ---- Naming ----
        public static string ToSafeName(string semaphoreName)
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
            // todo use SHA2
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
            GenericLockError = -50; // todo

        private static string CreateAcquireQuery()
        {
            // todo to improve cancellation story we should run 2 queries. The first will be the first part: non-blocking that should always succeed
            // in at least creating the table. This query should not be cancellable since we wouldn't know which table(s) would be created on it. The
            // second query will actually block/spin and should be cancellable. In the catch for that we can attempt cleanup.

            string GetSpinWaitSql(bool checkExpiry)
            {
                // todo handle reentrance here; we don't want to take the same lock twice!
                // todo instead of always looping from 0 and waiting on count-1, we should loop from rand [0, count)
                // and just wait on the countth lock. This is important for reentrance since if you own the last lock
                // then you'll spin without sleeping if we skip it
                
                // todo comments

                return $@"    
                    SET @waitCount = 0
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

                        IF APPLOCK_MODE('public', @{TicketLockNameParameter}, 'Session') = 'NoLock'
                        BEGIN
                            EXEC @lockResult = sys.sp_getapplock @{TicketLockNameParameter}, 'Exclusive', 'Session', {(checkExpiry ? "@lockTimeoutMillis" : "0")}
                            IF @lockResult >= 0
                            BEGIN
                                SET @{ResultCodeParameter} = {Success}
                                GOTO CODA
                            END
                            IF @lockResult < -1 GOTO CODA
                            SET @waitCount = @waitCount + 1
                        END
                        SET @i = @i + 1
                    END";
            }

            // todo combine declares

            // todo for transaction locks we should do all locking at the transaction level.
            // We can use DECLARE @lockScope NVARCHAR(32) = CASE @@TRANCOUNT WHEN 0 THEN 'Transaction' ELSE 'Session' END

            const string SpidCountSeparator = "s";

            return $@"
                DECLARE @lockResult INT
                DECLARE @initializationLock NVARCHAR(MAX) = 'init_' + @{SemaphoreNameParameter} + '_init'
                DECLARE @waiterNumber INT
                DECLARE @waiterCount INT
                DECLARE @i INT
                DECLARE @markerTableSql NVARCHAR(MAX)
                DECLARE @expiry DATETIME2
                DECLARE @lockTimeoutMillis INT
                DECLARE @busyWaitLock NVARCHAR(MAX) = 'bw_' + @{SemaphoreNameParameter} + '_busyWait'
                DECLARE @waitCount INT

                EXEC @lockResult = sys.sp_getapplock @initializationLock, 'Exclusive', 'Session', -1
                IF @lockResult < 0 GOTO CODA

                SELECT @waiterNumber = ISNULL(MAX(CAST(SUBSTRING(name, CHARINDEX('{SpidCountSeparator}', name, LEN(@{SemaphoreNameParameter})) + 1, LEN(name)) AS INT) + 1), 0),
                    @waiterCount = COUNT(*)
                FROM (SELECT * FROM tempdb.sys.tables WITH(NOLOCK)) x
                WHERE name LIKE '##' + REPLACE(REPLACE(REPLACE(@{SemaphoreNameParameter}, '\', '\\'), '_', '\_'), '%', '\%') + '%' ESCAPE '\'

                SET @{MarkerTableNameParameter} = 'tempdb..##' + @{SemaphoreNameParameter} + CAST(@@SPID AS NVARCHAR(MAX)) + '{SpidCountSeparator}' + CAST(@waiterNumber AS NVARCHAR(MAX))
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

                    IF SYSUTCDATETIME() > @expiry
                    BEGIN
                        SET @{ResultCodeParameter} = {LockTimeout}
                        GOTO CODA
                    END
                    IF @waitCount = 0
                        WAITFOR DELAY '00:00:00.005'
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
