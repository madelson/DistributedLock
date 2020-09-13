using System;
using System.Collections.Generic;
using System.Data;

namespace Medallion.Threading.Sql
{
    internal sealed class SqlApplicationLocks : ISqlSynchronizationStrategyMultiple<object>
    {
        public const int TimeoutExitCode = -1;

        public static readonly SqlApplicationLocks SharedLock = new SqlApplicationLocks(Mode.Shared),
            UpdateLock = new SqlApplicationLocks(Mode.Update),
            ExclusiveLock = new SqlApplicationLocks(Mode.Exclusive);

        private static readonly object Cookie = new object();
        private readonly Mode mode;

        private SqlApplicationLocks(Mode mode)
        {
            this.mode = mode;
        }

        bool ISqlSynchronizationStrategyMultiple<object>.IsUpgradeable => this.mode == Mode.Update;

        object? ISqlSynchronizationStrategyMultiple<object>.TryAcquire(ConnectionOrTransaction connectionOrTransaction, IEnumerable<string> resourceNames, int timeoutMillis)
        {
            return ExecuteAcquireCommand(connectionOrTransaction, resourceNames, timeoutMillis, this.mode) ? Cookie : null;
        }

        void ISqlSynchronizationStrategyMultiple<object>.Release(ConnectionOrTransaction connectionOrTransaction, IEnumerable<string> resourceNames, object lockCookie)
        {
            ExecuteReleaseCommand(connectionOrTransaction, resourceNames);
        }

        private static bool ExecuteAcquireCommand(ConnectionOrTransaction connectionOrTransaction, IEnumerable<string> lockNames, int timeoutMillis, Mode mode)
        {
            using var command = CreateAcquireCommand(connectionOrTransaction, lockNames, timeoutMillis, mode);
            var returnValue = (int)command.ExecuteScalar();
            return ParseExitCode(returnValue);
        }

        private static void ExecuteReleaseCommand(ConnectionOrTransaction connectionOrTransaction, IEnumerable<string> lockNames)
        {
            using var command = CreateReleaseCommand(connectionOrTransaction, lockNames);
            var result = (int)command.ExecuteScalar();
            ParseExitCode(result);
        }

        private static IDbCommand CreateAcquireCommand(
            ConnectionOrTransaction connectionOrTransaction, 
            IEnumerable<string> lockNames, 
            int timeoutMillis, 
            Mode mode)
        {
            if (lockNames == null)
                throw new NullReferenceException(nameof(lockNames));
            var names = string.Join(",", lockNames);
            var command = connectionOrTransaction.Connection!.CreateCommand();
            command.Transaction = connectionOrTransaction.Transaction;
            command.CommandText = @"
                BEGIN
                    DECLARE @lockReturnCodes table (
                        resourceName nvarchar(255),
                        code int
                    );
                    DECLARE @startLocation int = 1;
                    DECLARE @result int;
                    DECLARE @cmd nvarchar(max)
                    
                    BEGIN TRY
                        WHILE (@startLocation >= 1) 
                        BEGIN
                            DECLARE @resourceName nvarchar(255);
                            DECLARE @commaPos int = CHARINDEX(',', @ResourceNames, @startLocation);
                            IF (@commaPos > 0)
                                SET @resourceName = SUBSTRING(@resourceNames, @startLocation, @commaPos - @startLocation);
                            ELSE 
                            BEGIN
                                SET @resourceName = SUBSTRING(@resourceNames, @startLocation, LEN(@resourceNames) - @startLocation + 1);
                                SET @commapos = -1; -- This will cause break out of loop after execute command with the remainder of the string
                            END;
                            EXEC @result = dbo.sp_getapplock @Resource=@resourceName, @LockMode=@LockMode, @LockOwner=@LockOwner, @LockTimeout=@LockTimeout;
                            IF (@result < 0)
                                THROW 50001, 'Acquiring lock failed', 0; -- All or nothing
                            INSERT INTO @lockReturnCodes VALUES(@resourceName, @result);
                            SET @startLocation = @commapos + 1;
                        END
                    END TRY
                    BEGIN CATCH
                        -- Let's release all acquired locks on any error or on the first lock we failed to acquire
                        SET @cmd = '';
                        SELECT @cmd = @cmd + 'EXEC dbo.sp_releaseapplock @Resource=''' + resourceName 
                            + ''', @LockOwner=''' + @LockOwner + ''';'
                        FROM @lockReturnCodes;
                        EXEC sp_executesql @cmd;
                    END CATCH
                    SELECT @result
                END";
            command.CommandType = CommandType.Text;

            command.Parameters.Add(command.CreateParameter("LockOwner", command.Transaction != null ? "Transaction" : "Session"));
            command.Parameters.Add(command.CreateParameter("LockMode", GetModeString(mode)));
            command.Parameters.Add(command.CreateParameter("LockTimeout", timeoutMillis));
            command.Parameters.Add(command.CreateParameter("ResourceNames", names));
            
            command.CommandTimeout = timeoutMillis >= 0
                  // command timeout is in seconds. We always wait at least the lock timeout plus a buffer 
                  ? (timeoutMillis / 1000) + 30
                  // otherwise timeout is infinite so we use the infinite timeout of 0
                  // (see https://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqlcommand.commandtimeout%28v=vs.110%29.aspx)
                  : 0;

            return command;
        }

        private static IDbCommand CreateReleaseCommand(ConnectionOrTransaction connectionOrTransaction, IEnumerable<string> lockNames)
        {
            if (lockNames == null)
                throw new NullReferenceException(nameof(lockNames));
            var names = string.Join(",", lockNames);
            var command = connectionOrTransaction.Connection!.CreateCommand();
            command.Transaction = connectionOrTransaction.Transaction;
            command.CommandText = @"
                BEGIN
                    DECLARE @startLocation int = 1;
                    DECLARE @result int = 1;
                    DECLARE @tmpResult int;
                    
                    WHILE (@startLocation >= 1) 
                    BEGIN
                        DECLARE @resourceName nvarchar(255);
                        DECLARE @commaPos int = CHARINDEX(',', @ResourceNames, @startLocation);
                        IF (@commaPos > 0)
                            SET @resourceName = SUBSTRING(@resourceNames, @startLocation, @commaPos - @startLocation);			
                        ELSE 
                        BEGIN
                            SET @resourceName = SUBSTRING(@resourceNames, @startLocation, LEN(@resourceNames) - @startLocation + 1);
                            SET @commapos = -1; -- This will cause break out of loop after execute command with the remainder of the string
                        END;

                        EXEC @result = dbo.sp_releaseapplock @Resource=@resourceName, @LockOwner=@LockOwner;

                        IF (@tmpResult < @result)
                            SET @result = @tmpResult;
                        SET @startLocation = @commaPos + 1;
                    END	
                    SELECT @result
                END";
            command.CommandType = CommandType.Text;
            command.Parameters.Add(command.CreateParameter("ResourceNames", names));
            command.Parameters.Add(command.CreateParameter("LockOwner", command.Transaction != null ? "Transaction" : "Session"));
            
            return command;
        }

        public static bool ParseExitCode(int exitCode)
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

                default:
                    if (exitCode <= 0)
                        throw new InvalidOperationException(GetErrorMessage(exitCode, "unknown"));
                    return true; // unknown success code
            }
        }

        private static string GetErrorMessage(int exitCode, string type) => $"The request for the distribute lock failed with exit code {exitCode} ({type})";

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
}
