﻿using Medallion.Shell;
using NUnit.Framework;
using System.Net.Sockets;
using System.Text;

namespace Medallion.Threading.Tests.ZooKeeper;

[SetUpFixture]
public class ZooKeeperSetUpFixture
{
    private Command? _zooKeeperCommand;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        if (Environment.GetEnvironmentVariable("APPVEYOR") != null)
        {
            Console.WriteLine("Running on AppVeyor; will not attempt to launch ZooKeeper");
        }
        else if (IsZooKeeperRunning())
        {
            Console.WriteLine("ZooKeeper already running");
        }
        else
        {
            var zooKeeperHome = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "credentials", "zookeeper.txt")).Trim();
            if (!Directory.Exists(zooKeeperHome)) { throw new DirectoryNotFoundException(zooKeeperHome); }
            // On Windows, zkServer.cmd calls zkEnv.cmd, which checks for the environment variable JAVA_HOME.
            if (Environment.GetEnvironmentVariable("JAVA_HOME") is not { } javaHome || !Directory.Exists(javaHome))
            {
                throw new DirectoryNotFoundException("To run ZooKeeper, you should install Java Development Kit (JDK) and set the environment variable 'JAVA_HOME' based on that.");
            }
            var zooKeeperPath = Path.Combine(zooKeeperHome, "bin", "zkServer.cmd");

            var command = Command.Run(zooKeeperPath, options: o => o.StartInfo(i => i.RedirectStandardInput = false))
                .RedirectTo(Console.Out)
                .RedirectStandardErrorTo(Console.Error);
            Console.WriteLine($"Launched ZooKeeper ({zooKeeperPath}; PID={command.ProcessId})");
            this._zooKeeperCommand = command;
        }
    }

    private static bool IsZooKeeperRunning()
    {
        // based loosely on https://stackoverflow.com/questions/29106546/how-to-check-if-zookeeper-is-running-or-up-from-command-prompt

        try
        {
            using var tcpClient = new TcpClient("localhost", ZooKeeperPorts.DefaultPort);
            using var tcpStream = tcpClient.GetStream();
            var message = Encoding.UTF8.GetBytes("ruok");
            tcpStream.Write(message, 0, message.Length);

            tcpStream.ReadTimeout = 500; // ms
            var readBuffer = new byte[1024];
            var bytesRead = tcpStream.Read(readBuffer, 0, readBuffer.Length);
            var response = Encoding.UTF8.GetString(readBuffer, 0, bytesRead).Trim();
            if (response == "imok" || response == "ruok is not executed because it is not in the whitelist.")
            {
                return true;
            }

            throw new InvalidOperationException($"Received unexpected response '{response}' from application running at port {ZooKeeperPorts.DefaultPort}");
        }
        catch (SocketException) { }
        catch (IOException) { }

        return false;
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        if (this._zooKeeperCommand != null)
        {
            if (this._zooKeeperCommand.Task.IsCompleted)
            {
                throw new InvalidOperationException($"ZooKeeper exited unexpectedly with error code {this._zooKeeperCommand.Result.ExitCode}");
            }

            if (this._zooKeeperCommand.TrySignalAsync(CommandSignal.ControlC).Result
                && this._zooKeeperCommand.Task.Wait(TimeSpan.FromSeconds(2)))
            {
                return; // graceful shutdown
            }

            Console.WriteLine("ZooKeeper graceful shutdown failed: killing");
            this._zooKeeperCommand.Kill();
            this._zooKeeperCommand.Wait();
        }
    }
}
