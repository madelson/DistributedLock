using Medallion.Shell;
using NUnit.Framework;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Oracle
{
    // TODO consider changing this to leverage an Oracle cloud autonomous database OR just requiring
    // docker to be started manually; starting it automatically just doesn't seem to work that well.

    /// <summary>
    /// Starts and stops the oracle docker container if needed. For best performance, run "docker start oracle-distributed-lock" before running the
    /// tests so that the container will just keep running.
    /// 
    /// Note: for this setup we're assuming that the Oracle DB has been set up to run in Docker.
    /// 
    /// Setup steps:
    /// (1) Install Docker if needed
    /// (2) Clone https://github.com/oracle/docker-images
    /// (3) Follow instructions here: https://github.com/oracle/docker-images/tree/main/OracleDatabase/SingleInstance#oracle-database-container-images
    ///     for running the buildContainerImage shell script. I ran "./buildContainerImage.sh -v 18.4.0 -x" because as of 2021-11-01 that was the version that
    ///     came pre-baked with the repo
    /// (4) Create the container with "docker run --name oracle-distributed-lock -p 1521:1521 oracle/database:18.4.0-xe", then stop it (e. g. from the docker desktop gui)
    /// 
    /// NOTE: After reading https://blog.simonpeterdebbarma.com/2020-04-memory-and-wsl/ I also added a %USERPROFILE%/.wslconfig containing the following:
    /// [wsl2]
    /// memory=1GB
    /// </summary>
    [SetUpFixture]
    public class OracleSetUpFixture
    {
        private const string ContainerName = "oracle-distributed-lock";

        private static readonly Shell.Shell Shell = new(o => o.ThrowOnError(true));

        private bool _startedContainer;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // first, check if container has started
            //var containerLs = await Shell.Run("docker", new[] { "container", "ls" }).Task;
            //if (!containerLs.StandardOutput.Contains(ContainerName))
            //{
            //    Console.WriteLine($"docker container ls:");
            //    Console.WriteLine(containerLs);

            //    Console.WriteLine("STARTING CONTAINER");

            //    var stopwatch = Stopwatch.StartNew();
            //    await Shell.Run("docker", new[] { "start", ContainerName }).Task;
            //    this._startedContainer = true;
                
            //    Console.WriteLine($"CONTAINER STARTED ({stopwatch.Elapsed.TotalSeconds}s)");
            //}

            var timeout = Task.Delay(TimeSpan.FromMinutes(5));
            while (true)
            {
                var tryConnectTask = TryConnectAsync();
                var completed = Task.WaitAny(tryConnectTask, timeout);
                if (completed == 1) { throw new TimeoutException("Timed out trying to connect to Oracle"); }
                if (tryConnectTask.GetAwaiter().GetResult()) { return; }
            }

            static async Task<bool> TryConnectAsync()
            {
                await using var connection = new OracleConnection(OracleCredentials.GetConnectionString(TestContext.CurrentContext.TestDirectory));
                try
                {
                    await connection.OpenAsync();
                    return true;
                }
                catch (OracleException ex) 
                    when (
                        ex.Number == 12541 // TNS: No listener
                        || ex.Number == 12514 // TNS:listener does not currently know of service requested in connect descriptor
                        || ex.Number == 12528 // TNS: listener: all appropriate instances are blocking new connections
                        || ex.Number == 01033 // ORACLE initialization or shutdown in progress
                        || ex.Message.Contains("Connection request timed out")
                    )
                {
                    return false;
                }
            }
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            if (this._startedContainer)
            {
                Console.WriteLine("STOPPING CONTAINER");
                var stopwatch = Stopwatch.StartNew();
                await Shell.Run("docker", new[] { "stop", ContainerName }).Task;
                Console.WriteLine($"CONTAINER STOPPED ({stopwatch.Elapsed.TotalSeconds}s)");
            }
        }
    }
}
