using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Sql
{
    internal class IdleSessionKiller : IDisposable
    {
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly Task task;

        public IdleSessionKiller(string connectionString, TimeSpan idleTimeout)
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = this.cancellationTokenSource.Token;
            this.task = Task.Run(async () =>
            {
                var applicationName = new SqlConnectionStringBuilder(connectionString).ApplicationName;

                while (!cancellationToken.IsCancellationRequested)
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync(cancellationToken);
                        
                        var spidsToKill = new List<short>();
                        using (var findIdleSessionsCommand = connection.CreateCommand())
                        {
                            var expirationDate = DateTime.Now - idleTimeout;
                            findIdleSessionsCommand.CommandText = @"
                                SELECT session_id FROM sys.dm_exec_sessions
                                WHERE session_id != @@SPID
                                    AND login_name != 'sa'
                                    AND (last_request_start_time IS NULL OR last_request_start_time <= @expirationDate)
                                    AND (last_request_end_time IS NULL OR last_request_end_time <= @expirationDate)";
                            findIdleSessionsCommand.Parameters.Add(new SqlParameter("expirationDate", expirationDate));

                            try
                            {
                                using (var reader = await findIdleSessionsCommand.ExecuteReaderAsync(cancellationToken))
                                {
                                    while (await reader.ReadAsync(cancellationToken))
                                    {
                                        spidsToKill.Add(reader.GetInt16(0));
                                    }
                                }
                            }
                            catch (SqlException) when (cancellationToken.IsCancellationRequested)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                            }
                        }

                        foreach (var spid in spidsToKill)
                        {
                            using (var killCommand = connection.CreateCommand())
                            {
                                killCommand.CommandText = "KILL " + spid;
                                try { await killCommand.ExecuteNonQueryAsync(); }
                                catch (Exception ex) { Console.WriteLine($"Failed to kill {spid}: {ex}"); }
                            }
                        }

                        await Task.Delay(TimeSpan.FromTicks(idleTimeout.Ticks / 2), cancellationToken);
                    }
                }
            });
        }

        public void Dispose()
        {
            this.cancellationTokenSource.Cancel();

            // wait and swallow any OCE
            try { this.task.Wait(); }
            catch when (this.task.IsCanceled) { }

            this.cancellationTokenSource.Dispose();
        }
    }
}
