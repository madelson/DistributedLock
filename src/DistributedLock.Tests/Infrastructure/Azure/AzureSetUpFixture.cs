using Azure.Storage.Blobs;
using Medallion.Shell;
using NUnit.Framework;
using System.Diagnostics;

namespace Medallion.Threading.Tests.Azure;

[SetUpFixture]
public class AzureSetUpFixture
{
    private const string EmulatorProcessName = "AzureStorageEmulator";

    private bool _startedEmulator;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var existingProcesses = Process.GetProcessesByName(EmulatorProcessName);
        if (existingProcesses.Any())
        {
            Console.WriteLine($"Emulator already running (PID={existingProcesses[0].Id})");
            foreach (var process in existingProcesses) { process.Dispose(); }
        }
        else
        {
            var emulatorExePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft SDKs", "Azure", "Storage Emulator", $"{EmulatorProcessName}.exe");
            if (!File.Exists(emulatorExePath))
            {
                throw new FileNotFoundException($"Could not locate the {EmulatorProcessName} at {emulatorExePath}. This is required to run Azure tests. See https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator");
            }

            // Note: we used to hang on to this command to kill it later; we no longer do that because this process seems to naturally exit
            // by the end of the test and instead the emulator is running in another process. Therefore, we do a name-based lookup in teardown instead.
            var command = Command.Run(emulatorExePath, new[] { "start" }, o => o.StartInfo(i => i.RedirectStandardInput = false))
                .RedirectTo(Console.Out)
                .RedirectStandardErrorTo(Console.Error);
            Console.WriteLine($"Launched {EmulatorProcessName}");
            this._startedEmulator = true;
        }

        new BlobContainerClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName).CreateIfNotExists();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        new BlobContainerClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName).DeleteIfExists();

        if (this._startedEmulator)
        {
            foreach (var process in Process.GetProcessesByName(EmulatorProcessName))
            {
                process.Kill();
                process.WaitForExit();
            }
        }
    }
}
