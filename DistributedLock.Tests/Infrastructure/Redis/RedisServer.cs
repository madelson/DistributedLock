using Medallion.Shell;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Medallion.Threading.Tests.Redis
{
    internal class RedisServer : IDisposable
    {
        // redis default is 6379, so go one above that
        private static readonly int MinDynamicPort = RedisPorts.DefaultPorts.Max() + 1, MaxDynamicPort = MinDynamicPort + 100;

        private static readonly string WslPath = Directory.GetDirectories(@"C:\Windows\WinSxS")
            .Select(d => Path.Combine(d, "wsl.exe"))
            .Where(File.Exists)
            .OrderByDescending(File.GetCreationTimeUtc)
            .First();

        private static readonly Dictionary<int, RedisServer> ActiveServersByPort = new Dictionary<int, RedisServer>();
        private static readonly RedisServer[] DefaultServers = new RedisServer[RedisPorts.DefaultPorts.Count];

        private readonly Command _command;

        public RedisServer() : this(null) { }

        private RedisServer(int? port)
        {
            lock (ActiveServersByPort)
            {
                this.Port = port ?? Enumerable.Range(MinDynamicPort, count: MaxDynamicPort - MinDynamicPort + 1)
                    .First(p => !ActiveServersByPort.ContainsKey(p));
                this._command = Command.Run(WslPath, new object[] { "redis-server", "--port", this.Port }, options: o => o.StartInfo(si => si.RedirectStandardInput = false))
                    .RedirectTo(Console.Out)
                    .RedirectStandardErrorTo(Console.Error);
                ActiveServersByPort.Add(this.Port, this);
            }
            this.Multiplexer = ConnectionMultiplexer.Connect($"localhost:{this.Port}");
        }

        public int ProcessId => this._command.ProcessId;
        public int Port { get; }
        public ConnectionMultiplexer Multiplexer { get; }

        public void Dispose()
        {
            this.Multiplexer.Dispose();
            this._command.Kill();
            lock (ActiveServersByPort)
            {
                ActiveServersByPort.Remove(this.Port);
            }
        }

        public static RedisServer GetDefaultServer(int index)
        {
            lock (DefaultServers)
            {
                return DefaultServers[index] ??= new RedisServer(RedisPorts.DefaultPorts[index]);
            }
        }

        public static void DisposeAll()
        {
            lock (ActiveServersByPort)
            {
                ActiveServersByPort.Values.ToList().ForEach(s => s.Dispose());
            }
        }
    }
}
