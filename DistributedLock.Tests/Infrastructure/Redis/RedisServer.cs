using Medallion.Shell;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Redis
{
    internal class RedisServer
    {
        // redis default is 6379, so go one above that
        private static readonly int MinDynamicPort = RedisPorts.DefaultPorts.Max() + 1, MaxDynamicPort = MinDynamicPort + 100;

        // it's important for this to be lazy because it doesn't work when running on Linux
        private static readonly Lazy<string> WslPath = new Lazy<string>(
            () => Directory.GetDirectories(@"C:\Windows\WinSxS")
                .Select(d => Path.Combine(d, "wsl.exe"))
                .Where(File.Exists)
                .OrderByDescending(File.GetCreationTimeUtc)
                .First()
        );

        private static readonly Dictionary<int, RedisServer> ActiveServersByPort = new Dictionary<int, RedisServer>();
        private static readonly RedisServer[] DefaultServers = new RedisServer[RedisPorts.DefaultPorts.Count];

        private readonly Command _command;

        public RedisServer(bool allowAdmin = false) : this(null, allowAdmin) { }

        private RedisServer(int? port, bool allowAdmin)
        {
            lock (ActiveServersByPort)
            {
                this.Port = port ?? Enumerable.Range(MinDynamicPort, count: MaxDynamicPort - MinDynamicPort + 1)
                    .First(p => !ActiveServersByPort.ContainsKey(p));
                this._command = Command.Run(WslPath.Value, new object[] { "redis-server", "--port", this.Port }, options: o => o.StartInfo(si => si.RedirectStandardInput = false))
                    .RedirectTo(Console.Out)
                    .RedirectStandardErrorTo(Console.Error);
                ActiveServersByPort.Add(this.Port, this);
            }
            this.Multiplexer = ConnectionMultiplexer.Connect($"localhost:{this.Port}{(allowAdmin ? ",allowAdmin=true" : string.Empty)}");
            // Clean the db to ensure it is empty. Running an arbitrary command also ensures that 
            // the db successfully spun up before we proceed (Connect seemingly can complete before that happens). 
            // This is particularly important for cross-process locking where the lock taker process
            // assumes we've already started a server on certain ports.
            this.Multiplexer.GetDatabase().Execute("flushall", Array.Empty<object>(), CommandFlags.DemandMaster);
        }

        public int ProcessId => this._command.ProcessId;
        public int Port { get; }
        public ConnectionMultiplexer Multiplexer { get; }

        public static RedisServer GetDefaultServer(int index)
        {
            lock (DefaultServers)
            {
                return DefaultServers[index] ??= new RedisServer(RedisPorts.DefaultPorts[index], allowAdmin: false);
            }
        }

        public static void DisposeAll()
        {
            lock (ActiveServersByPort)
            {
                var shutdownTasks = ActiveServersByPort.Values
                    .Select(async server =>
                    {
                        server.Multiplexer.Dispose();
                        try
                        {
                            using var adminMultiplexer = await ConnectionMultiplexer.ConnectAsync($"localhost:{server.Port},allowAdmin=true");
                            adminMultiplexer.GetServer("localhost", server.Port).Shutdown(ShutdownMode.Never);
                        }
                        finally
                        {
                            if (!await server._command.Task.WaitAsync(TimeSpan.FromSeconds(5)))
                            {
                                server._command.Kill();
                                throw new InvalidOperationException("Forced to kill Redis server");
                            }
                        }
                    })
                    .ToArray();
                ActiveServersByPort.Clear();
                Task.WaitAll(shutdownTasks);
            }
        }
    }
}
