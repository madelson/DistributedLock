using Azure.Storage.Blobs;
using Medallion.Shell;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Medallion.Threading.Tests.Azure
{
    [SetUpFixture]
    public class AzureSetUpFixture
    {
        private Command? _azureStorageEmulatorCommand;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var existingProcesses = Process.GetProcessesByName("AzureStorageEmulator");
            if (existingProcesses.Any())
            {
                Console.WriteLine($"Emulator already running (PID={existingProcesses[0].Id})");
                foreach (var process in existingProcesses) { process.Dispose(); }
            }
            else
            {
                var emulatorExePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft SDKs", "Azure", "Storage Emulator", "AzureStorageEmulator.exe");
                if (!File.Exists(emulatorExePath))
                {
                    throw new FileNotFoundException($"Could not locate the AzureStorageEmulator at {emulatorExePath}. This is required to run Azure tests. See https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator");
                }

                var command = Command.Run(emulatorExePath, new[] { "start" }, o => o.StartInfo(i => i.RedirectStandardInput = false))
                    .RedirectTo(Console.Out)
                    .RedirectStandardErrorTo(Console.Error);
                Console.WriteLine($"Launched AzureStorageEmulator (PID={command.ProcessId})");
                this._azureStorageEmulatorCommand = command;
            }

            new BlobContainerClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName).CreateIfNotExists();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            new BlobContainerClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName).DeleteIfExists();

            if (this._azureStorageEmulatorCommand != null)
            {
                if (this._azureStorageEmulatorCommand.Task.IsCompleted)
                {
                    throw new InvalidOperationException($"AzureStorageEmulator exited unexpectedly with error code {this._azureStorageEmulatorCommand.Result.ExitCode}");
                }

                this._azureStorageEmulatorCommand.Kill();
                this._azureStorageEmulatorCommand.Wait();
            }
        }
    }
}
